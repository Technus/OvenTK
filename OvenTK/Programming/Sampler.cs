using System.Diagnostics;

namespace OvenTK.Lib.Programming;

internal class Sampler : IDisposable
{
    private bool _disposed;

    public int Handle { get; protected set; }

    protected Sampler(int handle)
    {
        Handle = handle;
    }

    public static implicit operator int(Sampler? data) => data?.Handle ?? default;

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

            try
            {
                GL.DeleteSampler(Handle);
            }
            catch (AccessViolationException e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                Handle = default;
            }

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
