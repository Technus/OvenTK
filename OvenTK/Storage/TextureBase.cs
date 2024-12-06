namespace OvenTK.Lib;

/// <summary>
/// Base clas for OpenGL object bindable to texture unit
/// </summary>
public abstract class TextureBase
{
    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }

    /// <summary>
    /// Texture type
    /// </summary>
    public TextureTarget Type { get; protected set; }

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(TextureBase? data) => data?.Handle ?? default;

    /// <summary>
    /// Activate texture<br/>
    /// Multiple textures can be bound, if your shader needs more than just one.<br/>
    /// If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.<br/>
    /// The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    /// </summary>
    /// <param name="unit"></param>
    public void Use(int unit)
    {
        GL.BindTextureUnit(unit, Handle);
    }

    /// <summary>
    /// Activate texture<br/>
    /// Multiple textures can be bound, if your shader needs more than just one.<br/>
    /// If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.<br/>
    /// The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="access"></param>
    /// <param name="format"></param>
    public void UseImage(int unit, TextureAccess access = TextureAccess.WriteOnly, SizedInternalFormat format = SizedInternalFormat.R32f)
    {
        GL.BindImageTexture(unit, Handle, 0, false, 0, access, format);
    }
}
