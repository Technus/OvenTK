namespace OvenTK.Lib;
/// <summary>
/// Wrapper for OpenGL sync
/// </summary>
/// <remarks>Usually using this incurs performance loss</remarks>
public readonly struct Sync : IDisposable
{
    private const ulong _timeoutIgnored = 0xFFFFFFFFFFFFFFFFul;

    /// <summary>
    /// OpenGL handle
    /// </summary>
    public nint Handle { get; }

    /// <summary>
    /// Use Factory methods
    /// </summary>
    /// <param name="handle"></param>
    public Sync(nint handle)
    {
        Handle = handle;
    }

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="sync"></param>
    public static implicit operator nint(Sync sync) => sync.Handle;

    /// <summary>
    /// Create the Sync
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    /// <remarks>currently the parameters are useless...</remarks>
    public static Sync Create(SyncCondition condition = SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags flags = WaitSyncFlags.None)
    {
        var handle = GL.FenceSync(condition, flags);
        return new(handle);
    }

    [Obsolete("Is not implemented in OpenGL")]
    public int IsSignaled()
    {
        GL.GetSync(Handle, SyncParameterName.SyncStatus, 1, out var _, out var status);
        return status;
    }

    /// <summary>
    /// Await sync for <paramref name="timeout"/>
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public bool Wait(TimeSpan timeout, WaitSyncFlags flags = WaitSyncFlags.None)
        => Wait(timeout == TimeSpan.MaxValue ? _timeoutIgnored : (ulong)(timeout.TotalMilliseconds * 1000000), flags);

    /// <summary>
    /// Await sync for <paramref name="timeout"/> milliseconds
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public bool Wait(ulong timeout = _timeoutIgnored, WaitSyncFlags flags = WaitSyncFlags.None)
    {
        GL.WaitSync(Handle, flags, timeout);
        var err = GL.GetError();
        return err switch
        {
            ErrorCode.NoError => true,
            _ => false
        };
    }

    /// <summary>
    /// Await sync for <paramref name="timeout"/>
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="flags"></param>
    /// <returns>the sync status</returns>
    public WaitSyncStatus WaitClient(TimeSpan timeout, ClientWaitSyncFlags flags = ClientWaitSyncFlags.SyncFlushCommandsBit)
        => WaitClient(timeout == TimeSpan.MaxValue ? _timeoutIgnored : (ulong)(timeout.TotalMilliseconds * 1000000), flags);

    /// <summary>
    /// Await sync for <paramref name="timeout"/> milliseconds
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="flags"></param>
    /// <returns>the sync status</returns>
    public WaitSyncStatus WaitClient(ulong timeout = _timeoutIgnored, ClientWaitSyncFlags flags = ClientWaitSyncFlags.SyncFlushCommandsBit) 
        => GL.ClientWaitSync(Handle, flags, timeout);

    /// <summary>
    /// Deletes the OpenGl Sync
    /// </summary>
    public void Dispose() => GL.DeleteSync(Handle);
}
