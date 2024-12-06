using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Size}:{Hint}")]
public class BufferData : BufferBase, IDisposable
{
    private const BufferUsageHint _default = BufferUsageHint.StaticDraw;
    private bool _disposed;

    public BufferUsageHint Hint { get; protected set; }

    protected BufferData(int handle, int byteSize, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        Handle = handle;
        Size = byteSize;
        Hint = hint;
        DrawType = drawType;
    }

    public void Resize(int size, DrawElementsType drawType = _drawTypeNone) => Resize(size, Hint, drawType);
    public void Resize(int size, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = size;
        DrawType = GetDrawType(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, default, hint);
    }

    public unsafe void Recreate<V>(ref readonly Memory<V> memory, DrawElementsType drawType = _drawTypeNone) => Recreate(in memory, Hint, drawType);
    public unsafe void Recreate<V>(ref readonly Memory<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory, DrawElementsType drawType = _drawTypeNone) => Recreate(in memory, Hint, drawType);
    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    public unsafe void Recreate<V>(ref readonly Span<V> memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);
    public unsafe void Recreate<V>(ref readonly Span<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);
    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(ref readonly V memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);
    public unsafe void Recreate<V>(ref readonly V memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = &memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(V[] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);
    public unsafe void Recreate<V>(V[] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    public unsafe void Recreate<V>(V[,] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);
    public unsafe void Recreate<V>(V[,] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    public unsafe void Recreate<V>(V[,,] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);
    public unsafe void Recreate<V>(V[,,] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }


    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static unsafe BufferData[] Create(IReadOnlyList<int> sizes, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        var ids = stackalloc int[sizes.Count];
        var buffers = new BufferData[sizes.Count];
        GL.CreateBuffers(sizes.Count, ids);
        for (int i = 0; i < sizes.Count; i++)
        {
            GL.NamedBufferData(ids[i], sizes[i], default, hint);
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
    public static IEnumerable<BufferData> Create(IEnumerable<int> sizes, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
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
    public static BufferData Create(int size = default, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferData(handle, size, default, hint);
        return new(handle, size, hint, drawType);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly Memory<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly Span<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly V memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed(V* p = &memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferData CreateFrom<V>(V[] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferData CreateFrom<V>(V[,] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    public static BufferData CreateFrom<V>(V[,,] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
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

    ~BufferData() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
