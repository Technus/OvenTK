using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// The OpenGL Data Buffer Wrapper
/// </summary>
[DebuggerDisplay("{Handle}:{Size}:{Hint}")]
public class BufferData : BufferBase, IDisposable
{
    internal const BufferUsageHint _default = BufferUsageHint.StaticDraw;

    private bool _disposed;

    /// <summary>
    /// The buffer usage hint
    /// </summary>
    public BufferUsageHint Hint { get; protected set; }

    /// <summary>
    /// Constructor to create it by hand, in normal cases use factory methods
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    protected BufferData(int handle, int size, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        Handle = handle;
        Size = size;
        Hint = hint;
        DrawType = drawType;
    }

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public new BufferData WithLabel(string label)
    {
        if (!Extensions._isDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Resize buffer to new <paramref name="size"/>, on the same OpenGL <see cref="BufferBase.Handle"/>
    /// </summary>
    /// <param name="size">in bytes</param>
    /// <param name="drawType"></param>
    public void Resize(int size, DrawElementsType drawType = _drawTypeNone) => Resize(size, Hint, drawType);

    /// <summary>
    /// Resize buffer to new <paramref name="size"/> and update hint, on the same OpenGL <see cref="BufferBase.Handle"/>
    /// </summary>
    /// <param name="size">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public void Resize(int size, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = size;
        DrawType = GetDrawType(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, default, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly Memory<V> memory, DrawElementsType drawType = _drawTypeNone) => Recreate(in memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly Memory<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory, DrawElementsType drawType = _drawTypeNone) => Recreate(in memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone)
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        using var pin = memory.Pin();
        GL.NamedBufferData(Handle, Size, (nint)pin.Pointer, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly Span<V> memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly Span<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly V memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(in memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(ref readonly V memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        fixed (V* p = &memory)
            GL.NamedBufferData(Handle, Size, (nint)p, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[,] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[,] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[,,] memory, DrawElementsType drawType = _drawTypeNone) where V : struct => Recreate(memory, Hint, drawType);

    /// <summary>
    /// Orphans the buffer and creates a new one on the same OpenGL <see cref="BufferBase.Handle"/><br/>
    /// Fills it with <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    public unsafe void Recreate<V>(V[,,] memory, BufferUsageHint hint, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        Size = memory.SizeOf();
        DrawType = GetDrawType<V>(drawType, _drawElementsType);
        GL.NamedBufferData(Handle, Size, memory, hint);
    }


    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="sizes">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData[] Create(IReadOnlyList<int> sizes, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        var ids = stackalloc int[sizes.Count];
        var buffers = new BufferData[sizes.Count];
        GL.CreateBuffers(sizes.Count, ids);
        for (int i = 0; i < sizes.Count; i++)
        {
            GL.NamedBufferData(ids[i], sizes[i], default, hint);
            buffers[i] = new(ids[i], sizes[i], hint, drawType);
        }
        return buffers;
    }

    /// <summary>
    /// Creates Buffers without data
    /// </summary>
    /// <param name="sizes">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static IEnumerable<BufferData> Create(IEnumerable<int> sizes, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        foreach (var size in sizes)
            yield return Create(size, hint, drawType);
    }

    /// <summary>
    /// Creates Buffer without data
    /// </summary>
    /// <param name="size">in bytes</param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferData Create(int size = default, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        GL.NamedBufferData(handle, size, default, hint);
        return new(handle, size, hint, drawType);
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData CreateFrom<V>(ref readonly Memory<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlyMemory<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone)
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        using var pin = memory.Pin();
        GL.NamedBufferData(handle, size, (nint)pin.Pointer, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData CreateFrom<V>(ref readonly Span<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData CreateFrom<V>(ref readonly ReadOnlySpan<V> memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed (V* p = memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static unsafe BufferData CreateFrom<V>(ref readonly V memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        fixed(V* p = &memory)
            GL.NamedBufferData(handle, size, (nint)p, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferData CreateFrom<V>(V[] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferData CreateFrom<V>(V[,] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Creates Buffer with data from <paramref name="memory"/>
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="memory"></param>
    /// <param name="hint"></param>
    /// <param name="drawType"></param>
    /// <returns></returns>
    public static BufferData CreateFrom<V>(V[,,] memory, BufferUsageHint hint = _default, DrawElementsType drawType = _drawTypeNone) where V : struct
    {
        GL.CreateBuffers(1, out int handle);
        var size = memory.SizeOf();
        GL.NamedBufferData(handle, size, memory, hint);
        return new(handle, size, hint, GetDrawType<V>(drawType));
    }

    /// <summary>
    /// Disposes buffer calls and deletes it
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Size = default;
                DrawType = _drawTypeNone;
            }

            try
            {
                GL.DeleteBuffer(Handle);
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

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~BufferData() => Dispose(disposing: false);

    /// <summary>
    /// Disposes the Buffer, using dispose pattern
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
