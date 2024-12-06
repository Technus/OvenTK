using Mapper;
using StbImageSharp;
using System.Diagnostics;

namespace OvenTK.Lib;
/// <summary>
/// A helper class, much like Shader, meant to simplify loading textures.
/// </summary>
/// <remarks>It is a 'sampler2D' texture</remarks>
[DebuggerDisplay("{Handle}:{Width}/{Height}")]
public class Texture : TextureBase, IDisposable, IImageInfo
{
    internal const int _mipDefault = 4;
    private bool _disposed;

    /// <summary>
    /// Texture width in px
    /// </summary>
    public int Width { get; private set; }
    /// <summary>
    /// Texture height in px
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    protected Texture(int handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public Texture WithLabel(string label)
    {
        if (!DebugExtensions.InDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Create texture from <paramref name="bytes"/>
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="mipLevels"></param>
    /// <returns></returns>
    public static Texture CreateFrom(byte[] bytes, int mipLevels = _mipDefault)
    {
        using var stream = new MemoryStream(bytes);
        return CreateFrom(stream, mipLevels);
    }

    /// <summary>
    /// Create texture from file on <paramref name="filePath"/>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="mipLevels"></param>
    /// <returns></returns>
    public static Texture CreateFrom(string filePath, int mipLevels = _mipDefault)
    {
        using var stream = File.OpenRead(filePath);
        return CreateFrom(stream, mipLevels);
    }

    /// <summary>
    /// Create texture from stream
    /// </summary>
    /// <param name="image"></param>
    /// <param name="mipLevels"></param>
    /// <param name="flipY">should it flip y for OpenGL, true by default</param>
    /// <returns></returns>
    public static Texture CreateFrom(Stream image, int mipLevels = _mipDefault, bool flipY = true)
    {
        // Generate handle
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int handle);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, mipLevels);

        var imageResult = image.LoadImageAndDispose(flipY);

        GL.TextureStorage2D(handle, mipLevels, SizedInternalFormat.Rgba8, imageResult.Width, imageResult.Height);
        GL.TextureSubImage2D(handle, 0, 0, 0, imageResult.Width, imageResult.Height, PixelFormat.Rgba, PixelType.UnsignedByte, imageResult.Data);

        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateTextureMipmap(handle);

        return new Texture(handle, imageResult.Width, imageResult.Height);
    }

    /// <summary>
    /// Helper method to pack images onto texture
    /// </summary>
    /// <param name="textures"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="mipLevels"></param>
    /// <returns></returns>
    public static Texture CreateFrom(IEnumerable<(int x, int y, ImageResult texture)> textures, int width, int height, int mipLevels = _mipDefault)
    {
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int handle);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, mipLevels);
        GL.TextureStorage2D(handle, mipLevels, SizedInternalFormat.Rgba8, width, height);

        foreach (var (x, y, texture) in textures)
            GL.TextureSubImage2D(handle, 0, x, y, texture.Width, texture.Height, PixelFormat.Rgba, PixelType.UnsignedByte, texture.Data);

        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateTextureMipmap(handle);
        return new Texture(handle, width, height);
    }

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
    ~Texture() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, deletes OpenGL texture
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}