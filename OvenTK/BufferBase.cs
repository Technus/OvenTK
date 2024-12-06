﻿namespace OvenTK.Lib;

public abstract class BufferBase
{
    public int Handle { get; protected set; }
    public int Size { get; protected set; }

    public abstract void Resize(int size);

    public static implicit operator int(BufferBase? data) => data?.Handle ?? 0;

    public BufferMap<T> Map<T>(BufferAccess bufferAccess = BufferAccess.ReadWrite) where T : struct
    {
        var p = GL.MapNamedBuffer(Handle, bufferAccess);
        return new(this, p);
    }

    public readonly struct BufferMap<T>(BufferBase buffer, nint p) : IDisposable where T : struct
    {
        public nint Pointer => p;

        public BufferBase Buffer => buffer;

        public void Dispose() => GL.UnmapNamedBuffer(Buffer.Handle);

        public Span<T> Span() => Pointer.AsSpan<T>(Buffer.Size);

        public ReadOnlySpan<T> ReadOnlySpan() => Pointer.AsReadOnlySpan<T>(Buffer.Size);
    }
}
