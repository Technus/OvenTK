using System;
using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Index}")]
public class VertexArrayAttrib : IDisposable
{
    private const int _maxSize = 4;
    private bool _disposed;

    public int Index { get; protected set; }
    public int Divisor { get; protected set; }
    public int Size { get; protected set; }
    public VertexAttribType Type { get; protected set; }
    public bool Normalized { get; protected set; }
    public int Stride { get; protected set; }
    public BufferBase? Buffer { get; protected set; }

    protected VertexArrayAttrib(BufferBase? buffer, int size, VertexAttribType type, int stride, bool normalized, int divisor)
    {
        Buffer = buffer;
        Size = size;
        Type = type;
        Stride = stride;
        Normalized = normalized;
        Divisor = divisor;
    }

    public static implicit operator int(VertexArrayAttrib data) => data.Index;

    public static VertexArrayAttrib Create(int size, VertexAttribType type, int stride = default, bool normalized = false, int divisor = default)
        => Create(default, size, type, stride, normalized, divisor);

    public static VertexArrayAttrib Create(BufferBase? buffer, int size, VertexAttribType type, int stride = default, bool normalized = false, int divisor = default)
    {
        if (size > _maxSize)
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Size cannot exceed: {_maxSize}");
        return new(buffer, size, type, stride, normalized, divisor);
    }

    internal void Assign(VertexArray vertexArray, int index)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VertexArrayAttrib));

        if (Index != default)
            throw new InvalidOperationException("Already set");

        Index = index;
        GL.EnableVertexArrayAttrib(vertexArray, Index);
        GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, Normalized, 0);
        GL.VertexArrayAttribBinding(vertexArray, Index, Index);
        GL.VertexArrayVertexBuffer(vertexArray, Index, Buffer, default, Stride);
        GL.VertexArrayBindingDivisor(vertexArray, Index, Divisor);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }
            //Nothing
            _disposed = true;
        }
    }

    // ~VertexArrayAttrib() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
