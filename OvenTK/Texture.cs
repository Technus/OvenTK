using StbImageSharp;

namespace OvenTK.Lib;

// A helper class, much like Shader, meant to simplify loading textures.
public class Texture : IDisposable
{
    private bool _disposed;
    private int handle;
    private readonly int width;
    private readonly int height;

    protected Texture(int handle, int width, int height)
    {
        this.handle = handle;
        this.width = width;
        this.height = height;
    }

    public int Handle => handle;
    public int Width => width;
    public int Height => height;

    public static Texture LoadFromFile(string path, int mipLevels = 4)
    {
        // Generate handle
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int handle);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, mipLevels);

        // Bind the handle
        //GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        //GL.BindTexture(TextureTarget.Texture2D, handle);

        // For this example, we're going to use .NET's built-in System.Drawing library to load textures.

        // OpenGL has it's texture origin in the lower left corner instead of the top left corner,
        // so we tell StbImageSharp to flip the image when loading.
        StbImage.stbi_set_flip_vertically_on_load(1);

        // Here we open a stream to the file and pass it to StbImageSharp to load.
        var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        stream.Dispose();

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
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        GL.TextureStorage2D(handle, mipLevels, SizedInternalFormat.Rgba8, image.Width, image.Height);
        GL.TextureSubImage2D(handle, 0, 0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

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

        return new Texture(handle, image.Width, image.Height);
    }

    // Activate texture
    // Multiple textures can be bound, if your shader needs more than just one.
    // If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
    // The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    public void Use(int unit)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }

            GL.DeleteTextures(1, ref handle);
            handle = default;

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Texture()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}