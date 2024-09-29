namespace OvenTK.Lib;

public class Sampler : IDisposable
{
    private bool _disposed;

    protected Sampler(int handle)
    {
        Handle = handle;
    }

    public static implicit operator int(Sampler data) => data.Handle;

    public int Handle { get; protected set; }

    public static Sampler Create()
    {
        GL.CreateSamplers(1, out int handle);

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

            GL.DeleteSampler(Handle);
            Handle = default;

            _disposed = true;
        }
    }

    ~Sampler() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
