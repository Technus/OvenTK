namespace OvenTK.Lib;
public class Sync : IDisposable
{
    private bool _disposed;

    public nint Handle { get; protected set; }

    public Sync(nint handle)
    {
        Handle = handle;
    }

    public static Sync Create(SyncCondition condition = SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags flags = WaitSyncFlags.None)
    {
        var handle = GL.FenceSync(condition, flags);
        return new(handle);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }

            GL.DeleteSync(Handle);
            Handle = default;

            _disposed = true;
        }
    }

    ~Sync() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
