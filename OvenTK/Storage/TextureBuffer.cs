using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// A special buffer backed texture<br/>
/// </summary>
/// <remarks>It is a '?samplerBuffer' texture</remarks>
[DebuggerDisplay("{Handle}")]
public class TextureBuffer : TextureBase, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Backing buffer
    /// </summary>
    public BufferBase? Buffer { get; protected set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="buffer"></param>
    protected TextureBuffer(int handle, BufferBase? buffer = default)
    {
        Handle = handle;
        Buffer = buffer;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public TextureBuffer WithLabel(string label)
    {
        if (!DebugExtensions.InDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Creates texture based on a buffer
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