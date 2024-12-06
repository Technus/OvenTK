using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// A special buffer backed texture<br/>
/// </summary>
/// <remarks>It is a '?samplerBuffer' texture</remarks>
[DebuggerDisplay("{Handle}")]
public class TextureBuffer : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// OpenGl handle
    /// </summary>
    public int Handle { get; private set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    protected TextureBuffer(int handle)
    {
        Handle = handle;
    }

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(TextureBuffer? data) => data?.Handle ?? default;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static TextureBuffer CreateFrom(BufferBase buffer, SizedInternalFormat format = SizedInternalFormat.Rgba8)
    {
        // Generate handle
        GL.CreateTextures(TextureTarget.TextureBuffer, 1, out int handle);
        GL.TextureBuffer(handle, format, buffer);
        return new TextureBuffer(handle);
    }

    /// <summary>
    /// Activate texture (buffer)<br/>
    /// Multiple textures can be bound, if your shader needs more than just one.<br/>
    /// If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.<br/>
    /// The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    /// </summary>
    /// <param name="unit"></param>
    public void Use(int unit) => GL.BindTextureUnit(unit, Handle);

    /// <summary>
    /// Dispose pattern, will delete the texture but not the bound buffer
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

            GL.DeleteTexture(Handle);
            Handle = default;

            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~TextureBuffer() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, will delete the texture but not the buffer
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}