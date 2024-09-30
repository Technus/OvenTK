using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;

public static class Extensions
{
    public static void EnableDebug()
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
            if (type is DebugType.DebugTypeError)
                throw new InvalidOperationException(str);
        }

        GL.Enable(EnableCap.DebugOutput);
        GL.DebugMessageCallback(OnDebugMessage, default);
    }

    public static int SizeOf<T>(this ReadOnlySpan<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Span<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this ReadOnlyMemory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Memory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T _) where T : struct => Unsafe.SizeOf<T>();
}
