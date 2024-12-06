namespace OvenTK.Lib.Storage;

/// <summary>
/// Frame buffer Attachement
/// </summary>
public class FrameBufferAtt
{
    /// <summary>
    /// The type of attachment
    /// </summary>
    public FramebufferAttachment Type { get; private set; }

    /// <summary>
    /// The handle assigned to this attachment
    /// </summary>
    public int Handle { get; private set; }

    /// <summary>
    /// If initialized with texture wrapper it will be available here
    /// </summary>
    public TextureBase? Texture { get; private set; }

    /// <summary>
    /// If initialized with render buffer wrapper it will be available here
    /// </summary>
    public RenderBuffer? RenderBuffer { get; private set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="type"></param>
    /// <param name="handle"></param>
    /// <param name="texture"></param>
    /// <param name="renderBuffer"></param>
    public FrameBufferAtt(FramebufferAttachment type, int handle, TextureBase? texture = default, RenderBuffer? renderBuffer = default)
    {
        Type = type;
        Handle = handle;
        Texture = texture;
        RenderBuffer = renderBuffer;
    }

    /// <summary>
    /// Creates a <see cref="FrameBuffer"/> attachement wrapper for <paramref name="texture"/>
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static FrameBufferAtt CreateFrom(TextureBase texture, FramebufferAttachment type = FramebufferAttachment.Color) =>
        new(type, texture.Handle, texture: texture);


    /// <summary>
    /// Creates a <see cref="FrameBuffer"/> attachement wrapper for <paramref name="renderBuffer"/>
    /// </summary>
    /// <param name="renderBuffer"></param>
    /// <param name="type"></param>
    public static FrameBufferAtt CreateFrom(RenderBuffer renderBuffer, FramebufferAttachment type = FramebufferAttachment.Color) =>
        new(type, renderBuffer.Handle, renderBuffer: renderBuffer);

    /// <summary>
    /// Creates a <see cref="FrameBuffer"/> attachement wrapper for <paramref name="handle"/>
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <remarks>Cannot be used to create a frame buffer, only a helper to read existing frame buffer</remarks>
    public static FrameBufferAtt Create(int handle, FramebufferAttachment type = FramebufferAttachment.Color) =>
        new(type, handle);

    /// <summary>
    /// Check if the attachment enum value is actually referring to attachment type
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    public static bool IsAttachment(FramebufferAttachment attachment) => attachment switch
    {
        FramebufferAttachment.FrontLeft or
        FramebufferAttachment.FrontRight or
        FramebufferAttachment.BackLeft or
        FramebufferAttachment.BackRight or
        FramebufferAttachment.Aux0 or
        FramebufferAttachment.Aux1 or
        FramebufferAttachment.Aux2 or
        FramebufferAttachment.Aux3 or
        FramebufferAttachment.Color or
        FramebufferAttachment.Depth or
        FramebufferAttachment.Stencil or
        FramebufferAttachment.DepthStencilAttachment or
        FramebufferAttachment.ColorAttachment0 or
        FramebufferAttachment.ColorAttachment1 or
        FramebufferAttachment.ColorAttachment2 or
        FramebufferAttachment.ColorAttachment3 or
        FramebufferAttachment.ColorAttachment4 or
        FramebufferAttachment.ColorAttachment5 or
        FramebufferAttachment.ColorAttachment6 or
        FramebufferAttachment.ColorAttachment7 or
        FramebufferAttachment.ColorAttachment8 or
        FramebufferAttachment.ColorAttachment9 or
        FramebufferAttachment.ColorAttachment10 or
        FramebufferAttachment.ColorAttachment11 or
        FramebufferAttachment.ColorAttachment12 or
        FramebufferAttachment.ColorAttachment13 or
        FramebufferAttachment.ColorAttachment14 or
        FramebufferAttachment.ColorAttachment15 or
        FramebufferAttachment.ColorAttachment16 or
        FramebufferAttachment.ColorAttachment17 or
        FramebufferAttachment.ColorAttachment18 or
        FramebufferAttachment.ColorAttachment19 or
        FramebufferAttachment.ColorAttachment20 or
        FramebufferAttachment.ColorAttachment21 or
        FramebufferAttachment.ColorAttachment22 or
        FramebufferAttachment.ColorAttachment23 or
        FramebufferAttachment.ColorAttachment24 or
        FramebufferAttachment.ColorAttachment25 or
        FramebufferAttachment.ColorAttachment26 or
        FramebufferAttachment.ColorAttachment27 or
        FramebufferAttachment.ColorAttachment28 or
        FramebufferAttachment.ColorAttachment29 or
        FramebufferAttachment.ColorAttachment30 or
        FramebufferAttachment.ColorAttachment31 or
        FramebufferAttachment.DepthAttachment or
        FramebufferAttachment.StencilAttachment => true,
        FramebufferAttachment.MaxColorAttachments => false,
        _ => throw new ArgumentOutOfRangeException(nameof(attachment),attachment,"Undefined value"),
    };
}
