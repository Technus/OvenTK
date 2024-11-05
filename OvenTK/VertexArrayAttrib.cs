using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Index}")]
public class VertexArrayAttrib : IDisposable
{
    protected const VertexAttribType _attribTypeNone = default;

    private const int _maxSize = 4;
    private bool _disposed;

    public int Index { get; protected set; }
    public int Divisor { get; protected set; }
    public int Size { get; protected set; }
    public VertexAttribType Type { get; protected set; }
    public bool Normalize { get; protected set; }
    public int Stride { get; protected set; }
    public BufferBase? Buffer { get; protected set; }

    protected VertexArrayAttrib(BufferBase? buffer, int size, VertexAttribType type, int stride, bool normalize, int divisor)
    {
        Buffer = buffer;
        Size = size;
        Type = type;
        Stride = stride;
        Normalize = normalize;
        Divisor = divisor;
    }

    public static implicit operator int(VertexArrayAttrib data) => data.Index;

    public static VertexArrayAttrib Create(int size, VertexAttribType type, int stride = default, bool normalize = false, int divisor = default)
        => Create(default, size, type, stride, normalize, divisor);

    public static VertexArrayAttrib Create(BufferBase? buffer, int size, VertexAttribType type, int stride = default, bool normalize = false, int divisor = default)
    {
        if (size > _maxSize)
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Size cannot exceed: {_maxSize}");
        return new(buffer, size, type, stride is 0 ? GetStride(size, type) : stride, normalize, divisor);
    }

    internal void Assign(VertexArray vertexArray, int index)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VertexArrayAttrib));

        if (Index != default)
            throw new InvalidOperationException("Already set");

        Index = index;
        GL.EnableVertexArrayAttrib(vertexArray, Index);
        switch (Type)
        {
            case VertexAttribType.Byte:
            case VertexAttribType.UnsignedByte:
            case VertexAttribType.Short:
            case VertexAttribType.UnsignedShort:
            case VertexAttribType.Int:
            case VertexAttribType.UnsignedInt:
                if(Normalize)
                    GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, true, 0);
                else
                    GL.VertexArrayAttribIFormat(vertexArray, Index, Size, Type, 0);//VertexAttribIntegerType
                break;
            case VertexAttribType.Float:
            case VertexAttribType.HalfFloat:
                GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, Normalize, 0);
                break;
            case VertexAttribType.Double:
                if (Normalize)
                    GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, true, 0);
                else
                    GL.VertexArrayAttribLFormat(vertexArray, Index, Size, VertexAttribType.Double, 0);
                break;
            case VertexAttribType.Fixed:
            case VertexAttribType.UnsignedInt2101010Rev:
            case VertexAttribType.UnsignedInt10F11F11FRev:
            case VertexAttribType.Int2101010Rev:
                throw new NotSupportedException("I have no idea how to set it up");
        }
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

    public static int GetStride(int size, VertexAttribType type) => size * type switch
    {
        VertexAttribType.Byte or 
        VertexAttribType.UnsignedByte => 1,
        VertexAttribType.Short or 
        VertexAttribType.UnsignedShort or 
        VertexAttribType.HalfFloat => 2,
        VertexAttribType.Int or 
        VertexAttribType.UnsignedInt or
        VertexAttribType.UnsignedInt2101010Rev or 
        VertexAttribType.UnsignedInt10F11F11FRev or
        VertexAttribType.Int2101010Rev or 
        VertexAttribType.Fixed or 
        VertexAttribType.Float => 4,
        VertexAttribType.Double => 8,
        _ => throw new InvalidOperationException("Invalid format"),
    };
}
