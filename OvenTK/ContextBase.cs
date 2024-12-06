namespace OvenTK.Lib;

/// <summary>
/// Generic context holder
/// </summary>
public abstract class ContextBase
{
    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public virtual Scope Use()
    {
        return new Scope(this);
    }

    /// <summary>
    /// Called by disposing scope
    /// </summary>
    protected virtual void Done()
    {
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
