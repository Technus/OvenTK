using System.Diagnostics;

namespace OvenTK.Lib;

public class FrequencyCounter : IDisposable
{
    private const int _movingAverageSampleCount = 10;

    private readonly Queue<long> lastFrameTime = [];
    private bool disposedValue;

    public FrequencyCounter()
    {
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
    }

    public double Frequency { get; private set; }

    public void PushEvent()
    {
        if (disposedValue)
            return;
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
        if (lastFrameTime.Count < _movingAverageSampleCount)
            return;
        var lastFrameTimeTemp = lastFrameTime.Dequeue();
        var frameTimeSpan = GetElapsedTime(lastFrameTimeTemp, Stopwatch.GetTimestamp());
        Frequency = 1000D / frameTimeSpan.TotalMilliseconds * _movingAverageSampleCount;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            lastFrameTime.Clear();
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
        new((long)((endingTimestamp - startingTimestamp) * (10000000D / Stopwatch.Frequency)));
}
