using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Attributes.Count}")]
public class VertexArray : IDisposable
{
    private bool _disposed;

    public int Handle { get; protected set; }
    public IReadOnlyList<VertexArrayAttrib> Attributes { get; protected set; }

    protected VertexArray(int handle, IReadOnlyList<VertexArrayAttrib> attributes)
    {
        Handle = handle;
        Attributes = attributes;
    }

    public static implicit operator int(VertexArray? data) => data?.Handle ?? default;

    // A wrapper function that enables the shader program.
    public void Use()
    {
        GL.BindVertexArray(Handle);
    }

    public static VertexArray Create(BufferBase buffer, IEnumerable<VertexArrayAttrib> attributes)
        => Create(buffer, attributes.ToArray());

    public static VertexArray Create(BufferBase buffer, IReadOnlyList<VertexArrayAttrib> attributes)
    {
        GL.CreateVertexArrays(1, out int handle);
        GL.VertexArrayElementBuffer(handle, buffer.Handle);
        var vao = new VertexArray(handle, attributes);
        for (int i = 0; i < attributes.Count; i++)
            attributes[i].Assign(vao, i);
        return vao;
    }

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

    ~VertexArray() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
