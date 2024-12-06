using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Size}:{Hint}")]
public class BufferData : BufferBase, IDisposable
{
    private const BufferUsageHint _default = BufferUsageHint.StaticDraw;
    private bool _disposed;

    public BufferUsageHint Hint { get; protected set; }

    protected BufferData(int handle, int byteSize, BufferUsageHint hint = _default)
    {
        Handle = handle;
        Size = byteSize;
        Hint = hint;
    }

    public void Resize(int size) => Resize(size, Hint);
    public void Resize(int size, BufferUsageHint hint)
    {
        Size = size;
        GL.NamedBufferData(Handle, Size, default, hint);
    }

    public unsafe void Recreate<V>(ref readonly Memory<V> memory) => Recreate(in memory, Hint);
    public unsafe void Recreate<V>(ref readonly Memory<V> memory, BufferUsageHint hint)
    {
        Size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory) => Recreate(in memory, Hint);
    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint)
    {
        Size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    public unsafe void Recreate<V>(ref readonly Span<V> memory) where V : struct => Recreate(in memory, Hint);
    public unsafe void Recreate<V>(ref readonly Span<V> memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory) where V : struct => Recreate(in memory, Hint);
    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(ref readonly V memory) where V : struct => Recreate(in memory, Hint);
    public unsafe void Recreate<V>(ref readonly V memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        fixed (V* p = &memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    public unsafe void Recreate<V>(V[] memory) where V : struct => Recreate(memory, Hint);
    public unsafe void Recreate<V>(V[] memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    public unsafe void Recreate<V>(V[,] memory) where V : struct => Recreate(memory, Hint);
    public unsafe void Recreate<V>(V[,] memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    public unsafe void Recreate<V>(V[,,] memory) where V : struct => Recreate(memory, Hint);
    public unsafe void Recreate<V>(V[,,] memory, BufferUsageHint hint) where V : struct
    {
        Size = memory.SizeOf();
        GL.NamedBufferData(Handle, Size, memory, hint);
    }


    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static unsafe BufferData[] Create(IReadOnlyList<int> sizes, BufferUsageHint hint = _default)
    {
        var ids = stackalloc int[sizes.Count];
        var buffers = new BufferData[sizes.Count];
        GL.CreateBuffers(sizes.Count, ids);
        for (int i = 0; i < sizes.Count; i++)
        {
            GL.NamedBufferData(ids[i], sizes[i], default, hint);
            buffers[i] = new(ids[i], sizes[i], hint);
        }
        return buffers;
    }

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static IEnumerable<BufferData> Create(IEnumerable<int> sizes, BufferUsageHint hint = _default)
    {
        foreach (var size in sizes)
            yield return Create(size, hint);
    }

    /// <summary>
    /// Creates Buffer without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static BufferData Create(int size = default, BufferUsageHint hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferData(handle, size, default, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly Memory<V> memory, BufferUsageHint hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly Span<V> memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferData CreateFrom<V>(ref readonly V memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed(V* p = &memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static BufferData CreateFrom<V>(V[] memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    public static BufferData CreateFrom<V>(V[,] memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    public static BufferData CreateFrom<V>(V[,,] memory, BufferUsageHint hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Size = default;
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
