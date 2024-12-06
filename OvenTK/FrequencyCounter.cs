using System.Diagnostics;

namespace OvenTK.Lib;
/// <summary>
/// Naive FPS/TPS counter
/// </summary>
public class FrequencyCounter : IDisposable
{
    private const int _movingAverageSampleCount = 10;

    private readonly Queue<long> lastFrameTime = [];
    private bool disposedValue;

    /// <summary>
    /// Make new one
    /// </summary>
    public FrequencyCounter()
    {
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// Current measure frequency
    /// </summary>
    public double Frequency { get; private set; }

    /// <summary>
    /// Add new event to measurement Queue
    /// </summary>
    public void PushEvent()
    {
        if (disposedValue)
            return;
        lastFrameTime.Enqueue(Stopwatch.GetTimestamp());
        if (lastFrameTime.Count < _movingAverageSampleCount)
            return;
        var lastFrameTimeTemp = lastFrameTime.Dequeue();
        var frameTimeSpan = Extensions.GetElapsedTime(lastFrameTimeTemp, Stopwatch.GetTimestamp());
        Frequency = 1000D / frameTimeSpan.TotalMilliseconds * _movingAverageSampleCount;
    }

    /// <summary>
    /// Dispose with dispose pattern
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            lastFrameTime.Clear();
            disposedValue = true;
        }
    }

    /// <summary>
    /// Dispose with dispose pattern
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
