using System.Collections.Concurrent;

namespace OvenTK.Lib.Utility;

/// <summary>
/// Finalization for "thread local" OpenGL objects
/// </summary>
public static class FallbackFinalizer
{
    /// <summary>
    /// The finalization queue for <see cref="FinalizeByQueuing"/>
    /// </summary>
    public static ConcurrentQueue<(int id, Action<int> action)> Queue { get; } = [];

    /// <summary>
    /// Instantly try to finalize the OpenGL handle
    /// </summary>
    /// <remarks>The action parameters are OpenGL handle and its appropriate DeleteMethod</remarks>
    public static Action<int, Action<int>> FinalizeByTrying { get; } = static (int id, Action<int> action) =>
    {
        try
        {
            action(id);
        }
        catch (AccessViolationException e)
        {
            throw new InvalidOperationException("Possibly tried to dispose on invalid thread.", e);
        }
    };

    /// <summary>
    /// Enqueues item for finalization later
    /// </summary>
    /// <remarks>The action parameters are OpenGL handle and its appropriate DeleteMethod</remarks>
    public static Action<int, Action<int>> FinalizeByQueuing { get; } = static (int id, Action<int> action) =>
        Queue.Enqueue((id, action));

    /// <summary>
    /// Instantly try to finalize the OpenGL handle, can be set to perform custom actions like delegating that work to main thread, or queuing it for later
    /// </summary>
    /// <remarks>The action parameters are OpenGL handle and its appropriate DeleteMethod</remarks>
    public static Action<int, Action<int>> FinalizeLater { get; set; } = FinalizeByTrying;

    /// <summary>
    /// Empties the finalization queue, this needs to be called on the main thread if the <see cref="FinalizeByQueuing"/> method is used in <see cref="FinalizeLater"/>
    /// </summary>
    /// <remarks>should be called from the creating thread</remarks>
    public static void RunFinalizers()
    {
        try
        {
            while (Queue.TryDequeue(out (int id, Action<int> action) entry))
                entry.action(entry.id);
        }
        catch (AccessViolationException e)
        {
            throw new InvalidOperationException("Possibly tried to dispose on invalid thread.", e);
        }
    }
}
