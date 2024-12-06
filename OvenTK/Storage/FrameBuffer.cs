namespace OvenTK.Lib.Storage;

/// <summary>
/// Render Output buffer
/// </summary>
public class FrameBuffer : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// list of attributes (in locations)
    /// </summary>
    public IReadOnlyList<FrameBufferAtt> Attachments { get; protected set; }

    /// <summary>
    /// The current framebuffer id
    /// </summary>
    public static int CurrentId(FramebufferTarget target = FramebufferTarget.Framebuffer) =>
        GL.GetInteger(target switch
        {
            FramebufferTarget.ReadFramebuffer => GetPName.ReadFramebufferBinding,
            FramebufferTarget.DrawFramebuffer => GetPName.DrawFramebufferBinding,
            FramebufferTarget.Framebuffer => GetPName.FramebufferBinding,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Undefined value"),
        });

    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="attachments"></param>
    public FrameBuffer(int handle, IReadOnlyList<FrameBufferAtt> attachments)
    {
        Handle = handle;
        Attachments = attachments;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public FrameBuffer WithLabel(string label)
    {
        if (!DebugExtensions.InDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Creates a framebuffer
    /// </summary>
    /// <returns></returns>
    public static FrameBuffer Create(IReadOnlyList<FrameBufferAtt> attachments)
    {
        GL.CreateFramebuffers(1, out int handle);
        foreach (var att in attachments)
        {
            if (att.Texture is not null && att.RenderBuffer is not null)
                throw new InvalidOperationException("Cannot bind both texture and render buffer");

            if (att.RenderBuffer is not null)
                GL.NamedFramebufferRenderbuffer(handle, att.Type, att.RenderBuffer.Type, att.RenderBuffer.Handle);
            else if (att.Texture is not null)
                GL.NamedFramebufferTexture(handle, att.Type, att.Texture.Handle, 0);
            else
                throw new InvalidOperationException("Cannot bind no texture and no render buffer");
        }
        return new(handle, attachments);
    }

    /// <summary>
    /// Creates a framebuffer wrapper for current framebuffer
    /// </summary>
    /// <returns></returns>
    public static FrameBuffer CreateFromId(int id)
    {
        List<FrameBufferAtt> attachments = [];
        foreach (var att in EnumStorage<FramebufferAttachment>.EnumValues.Where(FrameBufferAtt.IsAttachment))
        {
            GL.GetNamedFramebufferAttachmentParameter(id, att, FramebufferParameterName.FramebufferAttachmentObjectName, out int attId);
            if(attId is not 0)
                attachments.Add(new(att,attId));
        }
        return new(id, attachments);
    }

    /// <summary>
    /// Creates a framebuffer wrapper for current framebuffer
    /// </summary>
    /// <returns></returns>
    public static FrameBuffer CreateFromCurrent(FramebufferTarget target = FramebufferTarget.Framebuffer) =>
        CreateFromId(CurrentId(target));

    /// <summary>
    /// Creates a framebuffer wrapper for current framebuffer
    /// </summary>
    /// <returns></returns>
    public static FrameBuffer CreateFromDefault() =>
        CreateFromId(default);

    /// <summary>
    /// Use to check the status
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public FramebufferStatus CheckStatus(FramebufferTarget target = FramebufferTarget.Framebuffer) =>
        GL.CheckNamedFramebufferStatus(Handle, target);

    /// <summary>
    /// Binds the framebuffer
    /// </summary>
    /// <param name="target"></param>
    public void Use(FramebufferTarget target = FramebufferTarget.Framebuffer) => 
        GL.BindFramebuffer(target, Handle);

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(FrameBuffer? data) => data?.Handle ?? default;

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

            GL.DeleteFramebuffer(Handle);
            Handle = default;

            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~FrameBuffer() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, deletes OpenGL texture
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
