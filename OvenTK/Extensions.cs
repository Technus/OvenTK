using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;
/// <summary>
/// Collection of helper methods
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Enables printing to Debug and/or throwing exceptions on OpenGL errors
    /// </summary>
    /// <param name="throwErrors"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void EnableDebug(bool throwErrors = true)
    {
        static unsafe void OnDebugMessage(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            var str = Encoding.UTF8.GetString((byte*)pMessage, length);
            Debug.WriteLine($"{source}:{type}:{id}:{severity}:{str}");
        }
        static unsafe void OnDebugMessageThrowing(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            var str = Encoding.UTF8.GetString((byte*)pMessage, length);
            Debug.WriteLine($"{source}:{type}:{id}:{severity}:{str}");
            if (type is DebugType.DebugTypeError)
                throw new InvalidOperationException(str);
        }

        GL.Enable(EnableCap.DebugOutput);
        GL.DebugMessageCallback(throwErrors ? OnDebugMessageThrowing : OnDebugMessage, default);
    }

    public static int SizeOf<T>(this BufferBase.Mapping<T> arr) where T : struct => arr.Buffer.Size;

    public static int SizeOf<T>(this BufferBase.RangeMapping<T> arr) where T : struct => arr.Buffer.Size;

    public static int SizeOf<T>(this ReadOnlySpan<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Span<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this ReadOnlyMemory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Memory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T _) where T : struct => Unsafe.SizeOf<T>();

    public static unsafe Span<T> AsSpan<T>(this nint p, int bytes) where T : struct => new((void*)p, bytes / sizeof(T));

    public static unsafe ReadOnlySpan<T> AsReadOnlySpan<T>(this nint p, int bytes) where T : struct => new((void*)p, bytes / sizeof(T));

    /// <summary>
    /// Makes vertexes for <see cref="MakeRectIndices"/> for rectangle based on 2 triangles<br/>
    /// in such a way that:
    /// <code>
    ///    uint xFlag = uint(gl_VertexID % 2);
    ///    uint yFlag = uint(gl_VertexID / 2);
    /// </code>
    /// can be used to make decisions for further processing
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public static float[] MakeRectVertices(float w, float h)
    {
        w /= 2;
        h /= 2;
        return [
            -w,-h,
            +w,-h,
            -w,+h,
            +w,+h,
        ];
    }

    /// <summary>
    /// Makes indices for <see cref="MakeRectVertices(float, float)"/> for rectangle based on 2 triangles<br/>
    /// in such a way that:
    /// <code>
    ///    uint xFlag = uint(gl_VertexID % 2);
    ///    uint yFlag = uint(gl_VertexID / 2);
    /// </code>
    /// can be used to make decisions for further processing
    /// </summary>
    /// <returns></returns>
    public static byte[] MakeRectIndices() =>
    [
        1,3,2,
        2,1,0,
    ];

    /// <summary>
    /// Helper for Stopwatch Elapsed timestamp difference calculation
    /// </summary>
    /// <param name="startingTimestamp"></param>
    /// <param name="endingTimestamp"></param>
    /// <returns></returns>
    public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
        new((long)((endingTimestamp - startingTimestamp) * (10000000D / Stopwatch.Frequency)));
}
