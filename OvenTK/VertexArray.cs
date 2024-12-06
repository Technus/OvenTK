using System.Diagnostics;

namespace OvenTK.Lib;
/// <summary>
/// Wrapper for OpenGl vertex array
/// </summary>
[DebuggerDisplay("{Handle}:{Attributes.Count}")]
public class VertexArray : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }
    /// <summary>
    /// list of attributes (in locations)
    /// </summary>
    public IReadOnlyList<VertexArrayAttrib> Attributes { get; protected set; }

    /// <summary>
    /// Use factory method
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="attributes"></param>
    protected VertexArray(int handle, IReadOnlyList<VertexArrayAttrib> attributes)
    {
        Handle = handle;
        Attributes = attributes;
    }

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(VertexArray? data) => data?.Handle ?? default;

    /// <summary>
    /// A wrapper function that enables the vertex array as current.
    /// </summary>
    public void Use() => GL.BindVertexArray(Handle);

    /// <summary>
    /// Creates the vertex array
    /// </summary>
    /// <param name="buffer">buffer of indices</param>
    /// <param name="attributes">definition of additional parameters (vertices,colors,other user data,etc.)</param>
    /// <returns></returns>
    public static VertexArray Create(BufferBase buffer, IReadOnlyList<VertexArrayAttrib> attributes)
    {
        GL.CreateVertexArrays(1, out int handle);
        GL.VertexArrayElementBuffer(handle, buffer.Handle);
        var vao = new VertexArray(handle, attributes);
        for (int i = 0; i < attributes.Count; i++)
            attributes[i].Assign(vao, i);
        return vao;
    }

    /// <summary>
    /// Dispose pattern, deletes the OpenGL vertex array
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }

            GL.DeleteVertexArray(Handle);
            Handle = default;
            Attributes = default!;

            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~VertexArray() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, deletes the OpenGL vertex array
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
