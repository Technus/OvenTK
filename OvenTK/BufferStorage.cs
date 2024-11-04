using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Size}:{Flags}")]
public class BufferStorage : BufferBase, IDisposable
{
    private const BufferStorageFlags _default = BufferStorageFlags.None;
    private bool _disposed;

    public BufferStorageFlags Flags { get; protected set; }

    protected BufferStorage(int handle, int byteSize, BufferStorageFlags flags = _default, DrawElementsType drawType = _drawTypeNone)
    {
        Handle = handle;
        Size = byteSize;
        Flags = flags;
        DrawType = drawType;
    }

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static unsafe BufferStorage[] Create(IReadOnlyList<int> sizes, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        var ids = stackalloc int[sizes.Count];
        var buffers = new BufferStorage[sizes.Count];
        GL.CreateBuffers(sizes.Count, ids);
        for (int i = 0; i < sizes.Count; i++)
        {
            GL.NamedBufferStorage(ids[i], sizes[i], default, hint);
            buffers[i] = new(ids[i], sizes[i], hint, drawType);
        }
        return buffers;
    }

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static IEnumerable<BufferStorage> Create(IEnumerable<int> sizes, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        foreach (var size in sizes)
            yield return Create(size, hint, drawType);
    }

    /// <summary>
    /// Creates Buffer without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static BufferStorage Create(int size, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferStorage(handle, size, default, hint);
        return new(handle, size, hint, drawType);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly Memory<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly Span<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly V memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = &memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferStorage CreateFrom<V>(V[] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferStorage CreateFrom<V>(V[,] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferStorage CreateFrom<V>(V[,,] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Size = default;
                DrawType = _drawTypeNone;
            }

            try
            {
                GL.DeleteBuffer(Handle);
            }
            catch (AccessViolationException e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                Handle = default;
            }

            _disposed = true;
        }
    }

    ~BufferStorage() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
