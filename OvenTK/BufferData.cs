using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Size}:{Hint}")]
public class BufferData : BufferBase, IDisposable
{
    private const BufferUsageHint _default = BufferUsageHint.StaticDraw;
    private bool _disposed;

    protected BufferData(int handle, int byteSize, BufferUsageHint hint = _default)
    {
        Handle = handle;
        Size = byteSize;
        Hint = hint;
    }

    public BufferUsageHint Hint { get; protected set; }

    public override void Resize(int size)
    {
        Size = size;
        GL.NamedBufferData(Handle, size, default, Hint);
    }

    public void CopyTo(BufferData other)
    {
        if (other.Size < Size)
            throw new InvalidOperationException($"Buffer cannot copy, Size: {Size}->{other.Size}");
        GL.CopyNamedBufferSubData(Handle, other.Handle, default, default, Size);
    }

    public unsafe void Write<V>(ref readonly Memory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly ReadOnlyMemory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly Span<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, default, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly ReadOnlySpan<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, default, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly V value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed(V* p = &value)
            GL.NamedBufferSubData(Handle, default, size, (nint)p);
    }

    public void Write<V>(V[] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, default, size, value);
    }

    public void Write<V>(V[,] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, default, size, value);
    }

    public void Write<V>(V[,,] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, default, size, value);
    }

    public unsafe void Read<V>(ref readonly Memory<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        using var pin = value.Pin();
        GL.GetNamedBufferSubData(Handle, default, size, (nint)pin.Pointer);
    }

    public unsafe void Read<V>(ref readonly Span<V> value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed (V* p = value)
            GL.GetNamedBufferSubData(Handle, default, size, (nint)p);
    }

    public unsafe void Read<V>(ref readonly V value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed(V* p = &value)
            GL.GetNamedBufferSubData(Handle, default, size, (nint)p);
    }

    public void Read<V>(V[] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, default, size, value);
    }

    public void Read<V>(V[,] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, default, size, value);
    }
    
    public void Read<V>(V[,,] value) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, default, size, value);
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
