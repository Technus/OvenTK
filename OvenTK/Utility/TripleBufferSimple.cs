namespace OvenTK.Lib;
#pragma warning disable CS8618, S2933

/// <summary>
/// Triple buffer implementation, no synchronization supports only single reader and writer
/// </summary>
/// <typeparam name="T"></typeparam>
public class TripleBufferSimple<T>
{

    private int _1st = 0;
    private int _2nd = 1;
    private int _3rd = 2;
    private int _stale = 1;

    private T _element0;
    private T _element1;
    private T _element2;

    /// <summary>
    /// Empty triple buffer ctor
    /// </summary>
    protected TripleBufferSimple() { }

    /// <summary>
    /// Create triple buffer using <paramref name="initialValues"/> to generate insances
    /// </summary>
    /// <param name="initialValues"></param>
    public TripleBufferSimple(Func<T> initialValues)
    {
        _element0 = initialValues();
        _element1 = initialValues();
        _element2 = initialValues();
    }

    /// <summary>
    /// Create triple buffer using <paramref name="initialValues"/> to generate insances
    /// </summary>
    /// <param name="initialValues"></param>
    public TripleBufferSimple(Func<int, T> initialValues)
    {
        _element0 = initialValues(0);
        _element1 = initialValues(1);
        _element2 = initialValues(2);
    }

    /// <summary>
    /// Get read access to current read page
    /// </summary>
    /// <returns></returns>
    public ReadAccess<T> Read() => new(this);
    /// <summary>
    /// Get write acces to current write page
    /// </summary>
    /// <returns></returns>
    public WriteAccess<T> Write() => new(this);

    /// <summary>
    /// Check if the read buffer still contains the same or uninitialized data
    /// </summary>
    public bool IsStale => _stale is 1;

    /// <summary>
    /// Helper to get read acces to buffer page
    /// </summary>
    /// <typeparam name="V"></typeparam>
    public readonly struct ReadAccess<V> : IDisposable
    {
        /// <summary>
        /// Page id
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Parent buffer
        /// </summary>
        public TripleBufferSimple<V> Buffers { get; }

        /// <summary>
        /// Used internally by the triple buffer
        /// </summary>
        /// <param name="buffer"></param>
        public ReadAccess(TripleBufferSimple<V> buffer)
        {
            Buffers = buffer;
            Id = Buffers.BeginRead();
        }

        /// <summary>
        /// Acces to buffer page
        /// </summary>
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
        /// <summary>
        /// Read only acces to buffer page
        /// </summary>
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

        /// <summary>
        /// Finishes the read operation
        /// </summary>
        public void Dispose() => Buffers.FinishRead();
    }

    /// <summary>
    /// Helper to get write acces to buffer page
    /// </summary>
    /// <typeparam name="V"></typeparam>
    public readonly struct WriteAccess<V> : IDisposable
    {
        /// <summary>
        /// Page id
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Parent buffer
        /// </summary>
        public TripleBufferSimple<V> Buffers { get; }

        /// <summary>
        /// Used internally by the triple buffer
        /// </summary>
        /// <param name="buffer"></param>
        public WriteAccess(TripleBufferSimple<V> buffer)
        {
            Buffers = buffer;
            Id = Buffers.BeginWrite();
        }

        /// <summary>
        /// Acces to buffer page
        /// </summary>
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
        /// <summary>
        /// Read only acces to buffer page
        /// </summary>
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

        /// <summary>
        /// Finishes the read operation
        /// </summary>
        public void Dispose() => Buffers.FinishWrite();
    }

    private int BeginRead() => _3rd;

    private bool FinishRead()
    {
        var s = Interlocked.Exchange(ref _stale, 1);
        if (s is 1)
            return true;
        _3rd = Interlocked.Exchange(ref _2nd, _3rd);
        return false;
    }

    private int BeginWrite()
    {
        return _1st;
    }

    private bool FinishWrite()
    {
        _1st = Interlocked.Exchange(ref _2nd, _1st);
        Interlocked.MemoryBarrier();
        var s = Interlocked.Exchange(ref _stale, 0);
        return s is 0;
    }
}
