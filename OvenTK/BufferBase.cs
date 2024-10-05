namespace OvenTK.Lib;

public abstract class BufferBase
{
    public int Handle { get; protected set; }
    public int Size { get; protected set; }

    public static implicit operator int(BufferBase? data) => data?.Handle ?? 0;

    public void Invalidate() => GL.InvalidateBufferData(Handle);

    public void Invalidate(nint offset, int length) => GL.InvalidateBufferSubData(Handle, offset, length);

    public void CopyTo(BufferBase other, nint readOffset = default, nint writeOffset = default)
    {
        if (other.Size < Size)
            throw new InvalidOperationException($"Buffer cannot copy, Size: {Size}->{other.Size}");
        GL.CopyNamedBufferSubData(Handle, other.Handle, readOffset, writeOffset, Size);
    }

    public unsafe void Write<V>(ref readonly Memory<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, writeOffset, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly ReadOnlyMemory<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, writeOffset, size, (nint)pin.Pointer);
    }

    public unsafe void Write<V>(ref readonly Span<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly ReadOnlySpan<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    public unsafe void Write<V>(ref readonly V value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = &value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    public void Write<V>(V[] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    public void Write<V>(V[,] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    public void Write<V>(V[,,] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    public unsafe void Read<V>(ref readonly Memory<V> value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        using var pin = value.Pin();
        GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)pin.Pointer);
    }

    public unsafe void Read<V>(ref readonly Span<V> value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed (V* p = value)
            GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)p);
    }

    public unsafe void Read<V>(ref readonly V value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed (V* p = &value)
            GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)p);
    }

    public void Read<V>(V[] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    public void Read<V>(V[,] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    public void Read<V>(V[,,] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    public BufferMap<T> Map<T>(BufferAccess bufferAccess = BufferAccess.ReadWrite) where T : struct
    {
        var p = GL.MapNamedBuffer(Handle, bufferAccess);
        return new(this, p);
    }

    public BufferRangeMap<T> Map<T>(nint offset, int length, BufferAccessMask bufferAccess = BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit) where T : struct
    {
        var p = GL.MapNamedBufferRange(Handle, offset, length, bufferAccess);
        return new(this, p, offset, length);
    }

    public readonly struct BufferMap<T>(BufferBase buffer, nint p) : IDisposable where T : struct
    {
        public nint Pointer => p;
        public BufferBase Buffer => buffer;

        /// <summary>
        /// For explicitly mapped buffers
        /// </summary>
        public void Flush(nint offset = default, int length = default) 
            => GL.FlushMappedNamedBufferRange(Buffer.Handle, offset, length is 0 ? Buffer.Size - offset : length);
        /// <summary>
        /// For explicitly mapped buffers
        /// </summary>
        /// <remarks>It adds offset... of 0...</remarks>
        public void FlushRelative(nint offset = default, int length = default) 
            => GL.FlushMappedNamedBufferRange(Buffer.Handle, offset, length is 0 ? Buffer.Size - offset : length);
        public void Dispose() => GL.UnmapNamedBuffer(Buffer.Handle);

        public BufferRangeMap<T> BufferRangeMap() => new(Buffer, Pointer, default, Buffer.Size);
        public Span<T> Span() => Pointer.AsSpan<T>(Buffer.Size);
        public ReadOnlySpan<T> ReadOnlySpan() => Pointer.AsReadOnlySpan<T>(Buffer.Size);

        public static implicit operator BufferRangeMap<T>(BufferMap<T> map) => new(map.Buffer, map.Pointer, default, map.Buffer.Size);
        public static implicit operator Span<T>(BufferMap<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
        public static implicit operator ReadOnlySpan<T>(BufferMap<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
    }

    public readonly struct BufferRangeMap<T>(BufferBase buffer, nint p, nint offset, int size) : IDisposable where T : struct
    {
        public nint Pointer => p;
        public nint Offset => offset;
        public int Size => size;
        public BufferBase Buffer => buffer;

        /// <summary>
        /// For explicitly mapped buffers
        /// </summary>
        public void Flush(nint offset = default, int length = default) 
            => GL.FlushMappedNamedBufferRange(Buffer.Handle, offset, length is 0 ? Size - offset : length);
        /// <summary>
        /// For explicitly mapped buffers
        /// </summary>
        /// <remarks>It adds <see cref="Offset"/></remarks>
        public void FlushRelative(nint offset = default, int length = default) 
            => GL.FlushMappedNamedBufferRange(Buffer.Handle, offset + Offset, length is 0 ? Size - offset : length);
        public void Dispose() => GL.UnmapNamedBuffer(Buffer.Handle);

        public Span<T> Span() => Pointer.AsSpan<T>(Size);
        public ReadOnlySpan<T> ReadOnlySpan() => Pointer.AsReadOnlySpan<T>(Size);

        public static implicit operator Span<T>(BufferRangeMap<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
        public static implicit operator ReadOnlySpan<T>(BufferRangeMap<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
    }
}
