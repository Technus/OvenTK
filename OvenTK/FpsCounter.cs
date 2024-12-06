using System.Diagnostics;

namespace OvenTK.Lib;

public class FpsCounter : IDisposable
{
    private const int _movingAverageSampleCount = 60;

    private readonly Queue<long> lastFrameTime = [];
    private bool disposedValue;

    public FpsCounter()
    {
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
    }

    public double FPS { get; private set; }

    public void PushFrame()
    {
        if (disposedValue)
            return;
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
        if (lastFrameTime.Count < _movingAverageSampleCount)
            return;
        var lastFrameTimeTemp = lastFrameTime.Dequeue();
        var frameTimeSpan = GetElapsedTime(lastFrameTimeTemp, Stopwatch.GetTimestamp());
        FPS = 1000D / frameTimeSpan.TotalMilliseconds * _movingAverageSampleCount;
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
