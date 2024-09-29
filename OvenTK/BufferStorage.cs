namespace OvenTK.Lib;

public class BufferStorage : IDisposable
{
    private const BufferStorageFlags _default = BufferStorageFlags.None;
    private readonly BufferStorageFlags flags;
    private bool _disposed;
    private int handle;
    private int byteSize;

    protected BufferStorage(int handle, int byteSize, BufferStorageFlags flags = _default)
    {
        this.handle = handle;
        this.byteSize = byteSize;
        this.flags = flags;
    }

    public int Handle => handle;
    public int Size => byteSize;
    public BufferStorageFlags Flags => flags;

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="size"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static unsafe BufferStorage[] Create(IReadOnlyList<int> sizes, BufferStorageFlags hint = _default)
    {
        var ids = stackalloc int[sizes.Count];
        var buffers = new BufferStorage[sizes.Count];
        GL.CreateBuffers(sizes.Count, ids);
        for (int i = 0; i < sizes.Count; i++)
        {
            GL.NamedBufferStorage(ids[i], sizes[i], default, hint);
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
    public static IEnumerable<BufferStorage> Create(IEnumerable<int> sizes, BufferStorageFlags hint = _default)
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
    public static BufferStorage Create(int size, BufferStorageFlags hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferStorage(handle, size, default, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly Memory<V> memory, BufferStorageFlags hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferStorageFlags hint = _default)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferStorage(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly Span<V> memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static unsafe BufferStorage CreateFrom<V>(ref readonly V memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = &memory)
            GL.NamedBufferStorage(handle, size, (nint)p, hint);
        return new(handle, size, hint);
    }

    public static BufferStorage CreateFrom<V>(V[] memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    public static BufferStorage CreateFrom<V>(V[,] memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    public static BufferStorage CreateFrom<V>(V[,,] memory, BufferStorageFlags hint = _default) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferStorage(handle, size, memory, hint);
        return new(handle, size, hint);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                byteSize = default;
            }

            GL.DeleteBuffers(1, ref handle);
            handle = default;

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
