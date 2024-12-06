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
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public VertexArray WithLabel(string label)
    {
        if (!Extensions._isDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, Handle, -1, label);
        return this;
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
    /// <param name="elementBuffer">buffer of indices</param>
    /// <param name="attributes">definition of additional parameters (vertices,colors,other user data,etc.)</param>
    /// <returns></returns>
    public static VertexArray Create(BufferBase elementBuffer, IReadOnlyList<VertexArrayAttrib> attributes)
    {
        var vao = Create(attributes);
        GL.VertexArrayElementBuffer(vao.Handle, elementBuffer.Handle);
        return vao;
    }

    /// <summary>
    /// Creates the vertex array
    /// </summary>
    /// <param name="attributes">definition of additional parameters (vertices,colors,other user data,etc.)</param>
    /// <returns></returns>
    public static VertexArray Create(IReadOnlyList<VertexArrayAttrib> attributes)
    {
        GL.CreateVertexArrays(1, out int handle);
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
