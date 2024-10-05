namespace OvenTK.Lib;

public class TripleBufferCollaborative
{
    private readonly SemaphoreSlim _staleSemaphore = new(1, 1);
    private readonly SemaphoreSlim _readsSemaphore = new(1, 1);
    private readonly SemaphoreSlim _writesSemaphore = new(1, 1);
    private int _reads = 0;
    private int _writes = 0;

    private int _1st = 0;
    private int _2nd = 1;
    private int _3rd = 2;
    private int _stale = 1;

    public int BeginRead()
    {
        try
        {
            _readsSemaphore.Wait();

            Interlocked.Increment(ref _reads);
            return _3rd;
        }
        finally
        {
            _readsSemaphore.Release();
        }
    }

    public bool FinishRead()
    {
        try
        {
            _readsSemaphore.Wait();

            var i = Interlocked.Decrement(ref _reads);
            if (i is < 0)
                throw new InvalidOperationException("Finished read called too often...");
            if (i is not 0)
                return false;
            try
            {
                _staleSemaphore.Wait();

                var s = Interlocked.Exchange(ref _stale, 1);
                if (s is 1)
                    return true;
                _3rd = Interlocked.Exchange(ref _2nd, _3rd);
                return false;
            }
            finally
            {
                _staleSemaphore.Release();
            }
        }
        finally
        {
            _readsSemaphore.Release();
        }
    }

    public int BeginUpdate()
    {
        try
        {
            _writesSemaphore.Wait();

            Interlocked.Increment(ref _writes);
            return _1st;
        }
        finally
        {
            _writesSemaphore.Release();
        }
    }

    public bool FinishUpdate()
    {
        try
        {
            _writesSemaphore.Wait();

            var i = Interlocked.Decrement(ref _writes);
            if (i is < 0)
                throw new InvalidOperationException("Finished update called too often...");
            if (i is not 0)
                return false;
            try
            {
                _staleSemaphore.Wait();

                _1st = Interlocked.Exchange(ref _2nd, _1st);
                Interlocked.MemoryBarrier();
                var s = Interlocked.Exchange(ref _stale, 0);
                return s is 0;
            }
            finally
            {
                _staleSemaphore.Release();
            }
        }
        finally
        {
            _writesSemaphore.Release();
        }
    }
}
