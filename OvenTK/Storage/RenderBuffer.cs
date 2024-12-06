using OvenTK.Lib.Utility;
using System.Diagnostics;

namespace OvenTK.Lib.Storage;

/// <summary>
/// Render Output buffer
/// </summary>
public class RenderBuffer : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }

    /// <summary>
    /// Texture type
    /// </summary>
    public RenderbufferTarget Type { get; protected set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="type"></param>
    public RenderBuffer(int handle, RenderbufferTarget type = RenderbufferTarget.Renderbuffer)
    {
        Handle = handle;
        Type = type;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public RenderBuffer WithLabel(string label)
    {
        if (!DebugExtensions.InDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Renderbuffer, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Creates a framebuffer
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="format"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static RenderBuffer Create(int width, int height, RenderbufferStorage format = RenderbufferStorage.Depth24Stencil8, RenderbufferTarget target = RenderbufferTarget.Renderbuffer)
    {
        GL.CreateRenderbuffers(1, out int handle);
        GL.NamedRenderbufferStorage(handle, format, width, height);
        return new(handle, target);
    }

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(RenderBuffer? data) => data?.Handle ?? default;

    /// <summary>
    /// Dispose pattern, deletes OpenGL texture
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                GL.DeleteRenderbuffer(Handle);
                Handle = default;
            }
            else
            {
                FallbackFinalizer.FinalizeLater(Handle, static handle => GL.DeleteRenderbuffer(handle));
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~RenderBuffer() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, deletes OpenGL texture
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
