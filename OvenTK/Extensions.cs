using StbImageSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace OvenTK.Lib;
/// <summary>
/// Collection of helper methods
/// </summary>
public static class Extensions
{
    internal static readonly double _log2 = Math.Log(2);

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

    /// <summary>
    /// Loads image from stream (not the raw pixels, a image file)
    /// </summary>
    /// <param name="image">stream to read and dispose</param>
    /// <param name="flipY">should flip vertically</param>
    /// <returns></returns>
    public static ImageResult LoadImage(this Stream image, bool flipY = true)
    {
        using var stream = image;
        // OpenGL has it's texture origin in the lower left corner instead of the top left corner,
        // so we tell StbImageSharp to flip the image when loading.
        StbImage.stbi_set_flip_vertically_on_load_thread(flipY ? 1 : 0);

        // Here we open a stream to the file and pass it to StbImageSharp to load.
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    /// <summary>
    /// Get enum description or to string value
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct, Enum
    {
        var str = enumValue.ToString();
        var memInfo = typeof(TEnum).GetMember(str);
        if (memInfo is null || memInfo.Length is 0)
            throw new InvalidOperationException($"Enum value: {enumValue} is no defined");
        var attribute = memInfo[0].GetCustomAttribute<DescriptionAttribute>(false);
        return attribute?.Description ?? throw new InvalidOperationException($"Enum value: {enumValue} has no description");
    }
}
