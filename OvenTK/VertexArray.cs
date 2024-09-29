using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}:{Attributes.Count}")]
public class VertexArray : IDisposable
{
    private bool _disposed;

    public int Handle { get; protected set; }
    public IReadOnlyList<VertexArrayAttrib> Attributes { get; protected set; }

    protected VertexArray(int handle)
    {
        Handle = handle;
        Attributes = [];
    }

    public static implicit operator int(VertexArray data) => data.Handle;

    public static VertexArray Create()
    {
        GL.CreateVertexArrays(1, out int handle);

        return new(handle);
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
