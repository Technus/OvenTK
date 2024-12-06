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

        // Now that our pixels are prepared, it's time to generate a texture. We do this with GL.TexImage2D.
        // Arguments:
        //   The type of texture we're generating. There are various different types of textures, but the only one we need right now is Texture2D.
        //   Level of detail. We can use this to start from a smaller mipmap (if we want), but we don't need to do that, so leave it at 0.
        //   Target format of the pixels. This is the format OpenGL will store our image with.
        //   Width of the image
        //   Height of the image.
        //   Border of the image. This must always be 0; it's a legacy parameter that Khronos never got rid of.
        //   The format of the pixels, explained above. Since we loaded the pixels as RGBA earlier, we need to use PixelFormat.Rgba.
        //   Data type of the pixels.
        //   And finally, the actual pixels.
        GL.TextureStorage2D(handle, mipLevels, SizedInternalFormat.Rgba8, imageResult.Width, imageResult.Height);
        GL.TextureSubImage2D(handle, 0, 0, 0, imageResult.Width, imageResult.Height, PixelFormat.Rgba, PixelType.UnsignedByte, imageResult.Data);

        // Now that our texture is loaded, we can set a few settings to affect how the image appears on rendering.

        // First, we set the min and mag filter. These are used for when the texture is scaled down and up, respectively.
        // Here, we use Linear for both. This means that OpenGL will try to blend pixels, meaning that textures scaled too far will look blurred.
        // You could also use (amongst other options) Nearest, which just grabs the nearest pixel, which makes the texture look pixelated if scaled too far.
        // NOTE: The default settings for both of these are LinearMipmap. If you leave these as default but don't generate mipmaps,
        // your image will fail to render at all (usually resulting in pure black instead).
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Now, set the wrapping mode. S is for the X axis, and T is for the Y axis.
        // We set this to Repeat so that textures will repeat when wrapped. Not demonstrated here since the texture coordinates exactly match
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // Next, generate mipmaps.
        // Mipmaps are smaller copies of the texture, scaled down. Each mipmap level is half the size of the previous one
        // Generated mipmaps go all the way down to just one pixel.
        // OpenGL will automatically switch between mipmaps when an object gets sufficiently far away.
        // This prevents moiré effects, as well as saving on texture bandwidth.
        // Here you can see and read about the morié effect https://en.wikipedia.org/wiki/Moir%C3%A9_pattern
        // Here is an example of mips in action https://en.wikipedia.org/wiki/File:Mipmap_Aliasing_Comparison.png
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