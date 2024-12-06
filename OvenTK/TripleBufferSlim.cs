namespace OvenTK.Lib;

/// <summary>
/// Triple buffer implementation no synchronization, supports multiple reader writers but user code must ensure correct operation
/// </summary>
/// <typeparam name="T"></typeparam>
public class TripleBufferSlim<T>
{
    private int _reads = 0;
    private int _writes = 0;

    private int _1st = 0;
    private int _2nd = 1;
    private int _3rd = 2;
    private int _stale = 1;

    private T _element0;
    private T _element1;
    private T _element2;

    public TripleBufferSlim() { }

    public TripleBufferSlim(Func<T> initialValues)
    {
        _element0 = initialValues();
        _element1 = initialValues();
        _element2 = initialValues();
    }

    public TripleBufferSlim(Func<int, T> initialValues)
    {
        _element0 = initialValues(0);
        _element1 = initialValues(1);
        _element2 = initialValues(2);
    }

    public TripleBufferSlim(T initialValues) => _element0 = _element1 = _element2 = initialValues;

    public TripleBufferSlim(ref readonly T initialValues) => _element0 = _element1 = _element2 = initialValues;

    public ReadAccess<T> Read() => new(this);

    public WriteAccess<T> Write() => new(this);

    public bool IsStale => _stale is 1;

    public readonly struct ReadAccess<V> : IDisposable
    {
        public int Id { get; }

        public TripleBufferSlim<V> Buffers { get; }

        public ReadAccess(TripleBufferSlim<V> buffer)
        {
            Buffers = buffer;
            Id = Buffers.BeginRead();
        }

        public ref V Buffer
        {
            get
            {
                switch (Id)
                {
                    case 0:
                        return ref Buffers._element0;
                    case 1:
                        return ref Buffers._element1;
                    default:
                        return ref Buffers._element2;
                }
            }
        }

        public ref readonly V Value
        {
            get
            {
                switch (Id)
                {
                    case 0:
                        return ref Buffers._element0;
                    case 1:
                        return ref Buffers._element1;
                    default:
                        return ref Buffers._element2;
                }
            }
        }

        public void Dispose() => Buffers.FinishRead();
    }

    public readonly struct WriteAccess<V> : IDisposable
    {
        public int Id { get; }

        public TripleBufferSlim<V> Buffers { get; }

        public WriteAccess(TripleBufferSlim<V> buffer)
        {
            Buffers = buffer;
            Id = Buffers.BeginWrite();
        }

        public ref V Buffer
        {
            get
            {
                switch (Id)
                {
                    case 0:
                        return ref Buffers._element0;
                    case 1:
                        return ref Buffers._element1;
                    default:
                        return ref Buffers._element2;
                }
            }
        }

        public ref readonly V Value
        {
            get
            {
                switch (Id)
                {
                    case 0:
                        return ref Buffers._element0;
                    case 1:
                        return ref Buffers._element1;
                    default:
                        return ref Buffers._element2;
                }
            }
        }

        public void Dispose() => Buffers.FinishWrite();
    }

    private int BeginRead()
    {
        Interlocked.Increment(ref _reads);
        return _3rd;
    }

    private bool FinishRead()
    {
        var i = Interlocked.Decrement(ref _reads);
        if (i is < 0)
            throw new InvalidOperationException("Finished read called too often...");
        if (i is not 0)
            return false;

        var s = Interlocked.Exchange(ref _stale, 1);
        if (s is 1)
            return true;
        _3rd = Interlocked.Exchange(ref _2nd, _3rd);
        return false;
    }

    private int BeginWrite()
    {
        Interlocked.Increment(ref _writes);
        return _1st;
    }

    private bool FinishWrite()
    {
        var i = Interlocked.Decrement(ref _writes);
        if (i is < 0)
            throw new InvalidOperationException("Finished update called too often...");
        if (i is not 0)
            return false;

        _1st = Interlocked.Exchange(ref _2nd, _1st);
        Interlocked.MemoryBarrier();
        var s = Interlocked.Exchange(ref _stale, 0);
        return s is 0;
    }
}
