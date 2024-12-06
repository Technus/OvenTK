namespace OvenTK.Lib;

/// <summary>
/// Generic context holder
/// </summary>
public abstract class ContextBase
{
    /// <summary>
    /// On <see cref="Use"/>
    /// </summary>
    public Action? Begin { get; set; }
    /// <summary>
    /// On <see cref="Scope"/> dispose
    /// </summary>
    public Action? Finish { get; set; }

    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public virtual Scope Use()
    {
        Begin?.Invoke();
        return new Scope(this);
    }

    /// <summary>
    /// Called by disposing scope
    /// </summary>
    protected virtual void Done()
    {
        Finish?.Invoke();
    }

    /// <summary>
    /// Context scope
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly ContextBase _context;

        /// <summary>
        /// Used by <see cref="Use"/>
        /// </summary>
        /// <param name="drawingContext"></param>
        public Scope(ContextBase drawingContext) => _context = drawingContext;

        /// <summary>
        /// Calls <see cref="Done"/>
        /// </summary>
        public void Dispose() => _context.Done();
    }
}
