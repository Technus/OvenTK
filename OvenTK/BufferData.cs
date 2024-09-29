namespace OvenTK.Lib;

public class BufferData : IDisposable
{
    private const BufferUsageHint _default = BufferUsageHint.StaticDraw;
    private readonly BufferUsageHint hint;
    private bool _disposed;
    private int handle;
    private int byteSize;

    protected BufferData(int handle, int byteSize, BufferUsageHint hint = _default)
    {
        this.handle = handle;
        this.byteSize = byteSize;
        this.hint = hint;
    }

    public int Handle => handle;
    public int Size => byteSize;
    public BufferUsageHint Hint => hint;

    public void Resize(int size)
    {
        byteSize = size;
        GL.NamedBufferData(handle, size, default, hint);
    }

    public void CopyTo(BufferData other)
    {
        if (other.Size < byteSize)
            throw new InvalidOperationException($"Buffer cannot copy, Size: {byteSize}->{other.Size}");
        GL.CopyNamedBufferSubData(handle, other.Handle, default, default, byteSize);
    }

    public unsafe void Write<V>(ref readonly Memory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly ReadOnlyMemory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly Span<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        fixed (V* p = value)
            GL.NamedBufferSubData(handle, default, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly ReadOnlySpan<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        fixed (V* p = value)
            GL.NamedBufferSubData(handle, default, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly V value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        fixed(V* p = &value)
            GL.NamedBufferSubData(handle, default, size, (nint)p);
    }

    public void Write<V>(V[] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        GL.NamedBufferSubData(handle, default, size, value);
    }

    public void Write<V>(V[,] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        GL.NamedBufferSubData(handle, default, size, value);
    }

    public void Write<V>(V[,,] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{byteSize}");
        GL.NamedBufferSubData(handle, default, size, value);
    }

    public unsafe void Read<V>(ref readonly Memory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        using var pin = value.Pin();
        GL.GetNamedBufferSubData(handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Read<V>(ref readonly Span<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        fixed (V* p = value)
            GL.GetNamedBufferSubData(handle, default, size, (nint)p);
    }

    public unsafe void Read<V>(ref readonly V value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        fixed(V* p = &value)
            GL.GetNamedBufferSubData(handle, default, size, (nint)p);
    }

    public void Read<V>(V[] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        GL.GetNamedBufferSubData(handle, default, size, value);
    }

    public void Read<V>(V[,] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        GL.GetNamedBufferSubData(handle, default, size, value);
    }
    
    public void Read<V>(V[,,] value) where V : struct
    {
        var size = value.SizeOf();
        if (byteSize < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {byteSize}->{size}");
        GL.GetNamedBufferSubData(handle, default, size, value);
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
    public static BufferData Create(int size, BufferUsageHint hint = _default)
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
                byteSize = default;
            }

            GL.DeleteBuffers(1, ref handle);
            handle = default;

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
