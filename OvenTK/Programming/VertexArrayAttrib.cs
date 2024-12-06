using System.Diagnostics;

namespace OvenTK.Lib;
/// <summary>
/// A definition of a 'in' parameter (usually vertex shader input) for OpenGl Vertex array 
/// </summary>
[DebuggerDisplay("{Index}")]
public class VertexArrayAttrib
{
    internal const VertexAttribType _attribTypeNone = default;

    private const int _maxSize = 4;

    /// <summary>
    /// The assigned index of this parameter
    /// </summary>
    public int Index { get; protected set; }
    /// <summary>
    /// Per how many Instances the value of this parameter should be used (0 means per vertex instead)
    /// </summary>
    public int Divisor { get; protected set; }
    /// <summary>
    /// Size from 1 to 4 (the vec size of this parameter)
    /// </summary>
    public int Size { get; protected set; }
    /// <summary>
    /// What type of data is stored in the buffer
    /// </summary>
    public VertexAttribType Type { get; protected set; }
    /// <summary>
    /// when loading into floating point values should the data be normalized to [0,1]?
    /// </summary>
    public bool Normalize { get; protected set; }
    /// <summary>
    /// Byte stride of a single entry
    /// </summary>
    public int Stride { get; protected set; }
    /// <summary>
    /// Backing buffer
    /// </summary>
    public BufferBase? Buffer { get; protected set; }

    /// <summary>
    /// Use factory method
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="size"></param>
    /// <param name="type"></param>
    /// <param name="stride"></param>
    /// <param name="normalize"></param>
    /// <param name="divisor"></param>
    protected VertexArrayAttrib(BufferBase? buffer, int size, VertexAttribType type, int stride, bool normalize, int divisor)
    {
        Buffer = buffer;
        Size = size;
        Type = type;
        Stride = stride;
        Normalize = normalize;
        Divisor = divisor;
    }

    /// <summary>
    /// Casts to <see cref="Index"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(VertexArrayAttrib data) => data.Index;

    /// <summary>
    /// To put a blank in the <see cref="VertexArray"/>
    /// </summary>
    /// <returns></returns>
    public static VertexArrayAttrib CreateSkip() => new(default, 0, 0, 0, false, 0);

    /// <summary>
    /// Create a parameter definition
    /// </summary>
    /// <param name="size"></param>
    /// <param name="type"></param>
    /// <param name="stride"></param>
    /// <param name="normalize"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static VertexArrayAttrib Create(int size, VertexAttribType type, int stride = default, bool normalize = false, int divisor = default)
        => Create(default, size, type, stride, normalize, divisor);

    /// <summary>
    /// Create a parameter definition
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="size"></param>
    /// <param name="type"></param>
    /// <param name="stride"></param>
    /// <param name="normalize"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static VertexArrayAttrib Create(BufferBase? buffer, int size, VertexAttribType type, int stride = default, bool normalize = false, int divisor = default)
    {
        if (size > _maxSize)
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Size cannot exceed: {_maxSize}");
        return new(buffer, size, type, stride is 0 ? GetStride(size, type) : stride, normalize, divisor);
    }

    internal void Assign(VertexArray vertexArray, int index)
    {
        if (Index is not 0)
            throw new InvalidOperationException("Already set");

        Index = index;

        switch (Type)
        {
            case VertexAttribType.Byte:
            case VertexAttribType.UnsignedByte:
            case VertexAttribType.Short:
            case VertexAttribType.UnsignedShort:
            case VertexAttribType.Int:
            case VertexAttribType.UnsignedInt:
                GL.EnableVertexArrayAttrib(vertexArray, Index);

                if (Normalize)
                    GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, true, 0);
                else
                    GL.VertexArrayAttribIFormat(vertexArray, Index, Size, Type, 0);//VertexAttribIntegerType
                break;

            case VertexAttribType.Float:
            case VertexAttribType.HalfFloat:
                GL.EnableVertexArrayAttrib(vertexArray, Index);

                GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, Normalize, 0);
                break;

            case VertexAttribType.Double:
                GL.EnableVertexArrayAttrib(vertexArray, Index);

                if (Normalize)
                    GL.VertexArrayAttribFormat(vertexArray, Index, Size, Type, true, 0);
                else
                    GL.VertexArrayAttribLFormat(vertexArray, Index, Size, Type, 0);//VertexAttribDoubleType
                break;

            case VertexAttribType.Fixed:
            case VertexAttribType.UnsignedInt2101010Rev:
            case VertexAttribType.UnsignedInt10F11F11FRev:
            case VertexAttribType.Int2101010Rev:
                throw new NotSupportedException("I have no idea how to set it up");

            case 0:
                GL.DisableVertexArrayAttrib(vertexArray, Index);//disable when type is default, it is a blank
                return;
        }
        GL.VertexArrayAttribBinding(vertexArray, Index, Index);
        GL.VertexArrayVertexBuffer(vertexArray, Index, Buffer, default, Stride);
        GL.VertexArrayBindingDivisor(vertexArray, Index, Divisor);
    }

    /// <summary>
    /// Helper to get stride (single entry byte width)
    /// </summary>
    /// <param name="size"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
