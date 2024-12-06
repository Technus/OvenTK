namespace OvenTK.Lib;

/// <summary>
/// Base class for GPU Data or Storage Buffers
/// </summary>
public abstract class BufferBase
{
    internal const DrawElementsType _drawTypeNone = default;

    /// <summary>
    /// The storage for <see cref="DrawType"/>
    /// </summary>
    protected DrawElementsType _drawElementsType = _drawTypeNone;

    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }
    /// <summary>
    /// Size in bytes
    /// </summary>
    public int Size { get; protected set; }
    /// <summary>
    /// If this buffer contains valid data for drawing indices this will return the indice type
    /// </summary>
    /// <remarks>Throws if not...</remarks>
    public DrawElementsType DrawType
    {
        get => _drawElementsType is not _drawTypeNone ? _drawElementsType : throw new InvalidOperationException("Unknown draw (indices) type");
        set => _drawElementsType = GetDrawType(value);
    }
    /// <summary>
    /// The draw count helper
    /// </summary>
    public int DrawCount => _drawElementsType switch
    {
        DrawElementsType.UnsignedByte => Size,
        DrawElementsType.UnsignedShort => Size / 2,
        DrawElementsType.UnsignedInt => Size / 4,
        _ => throw new InvalidOperationException("Unknown draw (indices) type"),
    };

    /// <summary>
    /// Implicit cast to <see cref="Handle"/> or 0
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(BufferBase? data) => data?.Handle ?? 0;

    /// <summary>
    /// Invalidates buffer in OpenGL
    /// </summary>
    public void Invalidate() => GL.InvalidateBufferData(Handle);

    /// <summary>
    /// Invalidates buffer region in OpenGL
    /// </summary>
    /// <param name="offset">in bytes</param>
    /// <param name="length">in bytes</param>
    public void Invalidate(nint offset, int length) => GL.InvalidateBufferSubData(Handle, offset, length);

    /// <summary>
    /// Copy one buffer to other with OpenGL
    /// </summary>
    /// <param name="other"></param>
    /// <param name="count">in bytes</param>
    /// <param name="readOffset">in bytes</param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void CopyTo(BufferBase other, nint count = -1, nint readOffset = default, nint writeOffset = default)
    {
        if (count is -1)
            count = other.Size - writeOffset < Size - readOffset ? Size - readOffset : other.Size - writeOffset;
        if (other.Size - writeOffset - count < 0 || Size - count - readOffset < 0)
            throw new InvalidOperationException($"Buffer cannot copy {count}, Remaining Size: {Size - readOffset}->{other.Size - writeOffset}");
        GL.CopyNamedBufferSubData(Handle, other.Handle, readOffset, writeOffset, count);
    }

    /// <summary>
    /// Copy the memory to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Write<V>(ref readonly Memory<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, writeOffset, size, (nint)pin.Pointer);
    }

    /// <summary>
    /// Copy the memory to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Write<V>(ref readonly ReadOnlyMemory<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        using var pin = value.Pin();
        GL.NamedBufferSubData(Handle, writeOffset, size, (nint)pin.Pointer);
    }

    /// <summary>
    /// Copy the span to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Write<V>(ref readonly Span<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    /// <summary>
    /// Copy the span to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Write<V>(ref readonly ReadOnlySpan<V> value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    /// <summary>
    /// Copy the value to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Write<V>(ref readonly V value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        fixed (V* p = &value)
            GL.NamedBufferSubData(Handle, writeOffset, size, (nint)p);
    }

    /// <summary>
    /// Copy the array to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Write<V>(V[] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    /// <summary>
    /// Copy the array to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Write<V>(V[,] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    /// <summary>
    /// Copy the array to the selected position
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="writeOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Write<V>(V[,,] value, nint writeOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot write {typeof(V).Name}, Size: {size}->{Size}");
        GL.NamedBufferSubData(Handle, writeOffset, size, value);
    }

    /// <summary>
    /// Read to memory
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Read<V>(ref readonly Memory<V> value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        using var pin = value.Pin();
        GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)pin.Pointer);
    }

    /// <summary>
    /// Read to span
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Read<V>(ref readonly Span<V> value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed (V* p = value)
            GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)p);
    }

    /// <summary>
    /// Read value
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Read<V>(ref readonly V value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        fixed (V* p = &value)
            GL.GetNamedBufferSubData(Handle, readOffset, size, (nint)p);
    }

    /// <summary>
    /// Read to array
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Read<V>(V[] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    /// <summary>
    /// Read to array
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Read<V>(V[,] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    /// <summary>
    /// Read to array
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="value"></param>
    /// <param name="readOffset">in bytes</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Read<V>(V[,,] value, nint readOffset = default) where V : struct
    {
        var size = value.SizeOf();
        if (Size < size)
            throw new InvalidOperationException($"Buffer cannot read {typeof(V).Name}, Size: {Size}->{size}");
        GL.GetNamedBufferSubData(Handle, readOffset, size, value);
    }

    /// <summary>
    /// Map buffer to span, using the <see cref="IDisposable"/> <see cref="Mapping{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bufferAccess"></param>
    /// <returns></returns>
    public Mapping<T> Map<T>(BufferAccess bufferAccess = BufferAccess.ReadWrite) where T : struct
    {
        var p = GL.MapNamedBuffer(Handle, bufferAccess);
        return new(this, p);
    }

    /// <summary>
    /// Map buffer range to span, using the <see cref="IDisposable"/> <see cref="Mapping{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <param name="bufferAccess"></param>
    /// <returns></returns>
    public RangeMapping<T> Map<T>(nint offset, int length, BufferAccessMask bufferAccess = BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit) where T : struct
    {
        var p = GL.MapNamedBufferRange(Handle, offset, length, bufferAccess);
        return new(this, p, offset, length);
    }

    /// <summary>
    /// Map buffer range to span, with paging computation helper, using the <see cref="IDisposable"/> <see cref="Mapping{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="page"></param>
    /// <param name="totalPages"></param>
    /// <param name="bufferAccess"></param>
    /// <returns></returns>
    public RangeMapping<T> MapPage<T>(int page, int totalPages, BufferAccessMask bufferAccess = BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit) where T : struct
    {
        var length = Size / totalPages;
        nint offset = length * page;
        var p = GL.MapNamedBufferRange(Handle, offset, length, bufferAccess);
        return new(this, p, offset, length);
    }

    /// <summary>
    /// Helper for buffer mapping
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buffer"></param>
    /// <param name="p"></param>
    public readonly struct Mapping<T>(BufferBase buffer, nint p) : IDisposable where T : struct
    {
        /// <summary>
        /// Pointer to data
        /// </summary>
        public nint Pointer => p;
        /// <summary>
        /// The parent buffer
        /// </summary>
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
        /// <summary>
        /// Unmaps the mapping
        /// </summary>
        public void Dispose() => GL.UnmapNamedBuffer(Buffer.Handle);

        /// <summary>
        /// Helper to get range mapping of this mapping
        /// </summary>
        public RangeMapping<T> RangeMap => new(Buffer, Pointer, default, Buffer.Size);
        /// <summary>
        /// Helper to get span of this mapping
        /// </summary>
        public Span<T> Span => Pointer.AsSpan<T>(Buffer.Size);
        /// <summary>
        /// Helper to get read only span of this mapping
        /// </summary>
        public ReadOnlySpan<T> ReadOnlySpan => Pointer.AsReadOnlySpan<T>(Buffer.Size);

        /// <summary>
        /// Cast to Range mapping using whole range
        /// </summary>
        /// <param name="map"></param>
        public static implicit operator RangeMapping<T>(Mapping<T> map) => new(map.Buffer, map.Pointer, default, map.Buffer.Size);
        /// <summary>
        /// Cast to span using whole range
        /// </summary>
        /// <param name="map"></param>
        public static implicit operator Span<T>(Mapping<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
        /// <summary>
        /// Cast to read only span using whole range
        /// </summary>
        /// <param name="map"></param>
        public static implicit operator ReadOnlySpan<T>(Mapping<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
    }

    /// <summary>
    /// Helper for partial buffer mapping
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buffer"></param>
    /// <param name="p"></param>
    /// <param name="offset"></param>
    /// <param name="size"></param>
    public readonly struct RangeMapping<T>(BufferBase buffer, nint p, nint offset, int size) : IDisposable where T : struct
    {
        /// <summary>
        /// Pointer to data
        /// </summary>
        public nint Pointer => p;
        /// <summary>
        /// Offset from <see cref="Buffer"/> start
        /// </summary>
        public nint Offset => offset;
        /// <summary>
        /// Size in bytes
        /// </summary>
        public int Size => size;
        /// <summary>
        /// The parent buffer
        /// </summary>
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
        /// <summary>
        /// Unmaps the mapping
        /// </summary>
        public void Dispose() => GL.UnmapNamedBuffer(Buffer.Handle);

        /// <summary>
        /// Helper to get span of this mapping
        /// </summary>
        public Span<T> Span => Pointer.AsSpan<T>(Size);
        /// <summary>
        /// Helper to get read only span of this mapping
        /// </summary>
        public ReadOnlySpan<T> ReadOnlySpan => Pointer.AsReadOnlySpan<T>(Size);

        /// <summary>
        /// Cast to span using this range
        /// </summary>
        /// <param name="map"></param>
        public static implicit operator Span<T>(RangeMapping<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
        /// <summary>
        /// Cast to read only span using this range
        /// </summary>
        /// <param name="map"></param>
        public static implicit operator ReadOnlySpan<T>(RangeMapping<T> map) => map.Pointer.AsSpan<T>(map.Buffer.Size);
    }

    /// <summary>
    /// Helper to get Drawing type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="requested"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    public static DrawElementsType GetDrawType<T>(DrawElementsType requested, DrawElementsType current)
    {
        if (requested is not _drawTypeNone)
            return requested;
        requested = GetDrawType<T>();
        if (requested is not _drawTypeNone)
            return requested;
        return current;
    }

    /// <summary>
    /// Helper to get Drawing type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="requested"></param>
    /// <returns></returns>
    public static DrawElementsType GetDrawType<T>(DrawElementsType requested) =>
        requested is _drawTypeNone ? GetDrawType<T>() : requested;

    /// <summary>
    /// Helper to get Drawing type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static DrawElementsType GetDrawType<T>() => Unsafe.SizeOf<T>() switch
    {
        1 => DrawElementsType.UnsignedByte,
        2 => DrawElementsType.UnsignedShort,
        4 => DrawElementsType.UnsignedInt,
        _ => _drawTypeNone,
    };

    /// <summary>
    /// Helper to get Drawing type
    /// </summary>
    /// <param name="requested"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    public static DrawElementsType GetDrawType(DrawElementsType requested, DrawElementsType current) =>
        requested is _drawTypeNone ? current : requested;

    /// <summary>
    /// Helper to get Drawing type
    /// </summary>
    /// <param name="requested"></param>
    /// <returns></returns>
    public static DrawElementsType GetDrawType(DrawElementsType requested) => requested switch
    {
        DrawElementsType.UnsignedByte => DrawElementsType.UnsignedByte,
        DrawElementsType.UnsignedShort => DrawElementsType.UnsignedShort,
        DrawElementsType.UnsignedInt => DrawElementsType.UnsignedInt,
        _ => _drawTypeNone,
    };
}
