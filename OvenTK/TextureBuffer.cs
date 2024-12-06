using System.Diagnostics;

namespace OvenTK.Lib;

// A helper class, much like Shader, meant to simplify loading textures.

[DebuggerDisplay("{Handle}")]
public class TextureBuffer : IDisposable
{
    private bool _disposed;

    public int Handle { get; private set; }

    protected TextureBuffer(int handle)
    {
        Handle = handle;
    }

    public static implicit operator int(TextureBuffer? data) => data?.Handle ?? default;

    public static TextureBuffer CreateFrom(BufferBase buffer, SizedInternalFormat format = SizedInternalFormat.Rgba8)
    {
        // Generate handle
        GL.CreateTextures(TextureTarget.TextureBuffer, 1, out int handle);
        GL.TextureBuffer(handle, format, buffer);
        return new TextureBuffer(handle);
    }

    // Activate texture
    // Multiple textures can be bound, if your shader needs more than just one.
    // If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
    // The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    public void Use(int unit)
    {
        GL.BindTextureUnit(unit, Handle);
    }

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

    ~TextureBuffer() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}