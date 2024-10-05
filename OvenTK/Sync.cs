namespace OvenTK.Lib;
public readonly struct Sync : IDisposable
{
    private const ulong _timeoutIgnored = 0xFFFFFFFFFFFFFFFFul;

    public nint Handle { get; }

    public Sync(nint handle)
    {
        Handle = handle;
    }

    public static implicit operator nint(Sync sync) => sync.Handle;

    public void Dispose() => GL.DeleteSync(Handle);

    [Obsolete]
    public int IsSignaled()
    {
        GL.GetSync(Handle, SyncParameterName.SyncStatus, 1, out var _, out var status);
        return status;
    }

    public bool Wait(TimeSpan timeout, WaitSyncFlags flags = WaitSyncFlags.None)
        => Wait(timeout == TimeSpan.MaxValue ? _timeoutIgnored : (ulong)(timeout.TotalMilliseconds * 1000000), flags);

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

    public WaitSyncStatus WaitClient(TimeSpan timeout, ClientWaitSyncFlags flags = ClientWaitSyncFlags.SyncFlushCommandsBit)
        => WaitClient(timeout == TimeSpan.MaxValue ? _timeoutIgnored : (ulong)(timeout.TotalMilliseconds * 1000000), flags);

    public WaitSyncStatus WaitClient(ulong timeout = _timeoutIgnored, ClientWaitSyncFlags flags = ClientWaitSyncFlags.SyncFlushCommandsBit) 
        => GL.ClientWaitSync(Handle, flags, timeout);

    public static Sync Create(SyncCondition condition = SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags flags = WaitSyncFlags.None)
    {
        var handle = GL.FenceSync(condition, flags);
        return new(handle);
    }
}
