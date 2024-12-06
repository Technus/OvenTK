using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// OpenGL storage buffer (of fixed size and no reallocation suppport) for const (size) data or something similar
/// </summary>
[DebuggerDisplay("{Handle}:{Size}:{Flags}")]
public class BufferStorage : BufferBase, IDisposable
{
    internal const BufferStorageFlags _default = BufferStorageFlags.None;

    private bool _disposed;

    /// <summary>
    /// The buffer storage flags
    /// </summary>
    public BufferStorageFlags Flags { get; protected set; }

    /// <summary>
    /// Constructor to create it by hand, in normal cases use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size">in bytes</param>
    /// <param name="flags"></param>
    /// <param name="drawType"></param>
    protected BufferStorage(int handle, int size, BufferStorageFlags flags = _default, DrawElementsType drawType = _drawTypeNone)
    {
        Handle = handle;
        Size = size;
        Flags = flags;
        DrawType = drawType;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public new BufferStorage WithLabel(string label)
    {
        if (!Extensions._isDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="sizes">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
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
    /// <param name="sizes">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
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
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferStorage Create(int size, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferStorage(handle, size, default, hint);
        return new(handle, size, hint, drawType);
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferStorage CreateFrom<V>(ref readonly Memory<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferStorage CreateFrom<V>(ref readonly Span<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferStorage CreateFrom<V>(ref readonly V memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = &memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferStorage CreateFrom<V>(V[] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferStorage CreateFrom<V>(V[,] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferStorage CreateFrom<V>(V[,,] memory, BufferStorageFlags hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Disposes buffer calls and deletes it
    /// </summary>
    /// <param name="disposing"></param>
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

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~BufferStorage() => Dispose(disposing: false);

    /// <summary>
    /// Disposes the Buffer, using dispose pattern
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
