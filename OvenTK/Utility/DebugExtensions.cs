using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;
/// <summary>
/// Helper for OpenGL Debugging
/// </summary>
public static class DebugExtensions
{
    /// <summary>
    /// is the OpenGL debug enabled or not
    /// </summary>
    public static bool InDebug { get; private set; } = false;

    /// <summary>
    /// Enables printing to Debug and/or throwing exceptions on OpenGL errors
    /// </summary>
    /// <param name="throwErrors"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void EnableDebug(bool throwErrors = true)
    {
#pragma warning disable S1172
        static unsafe void OnDebugMessage(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            var str = Encoding.ASCII.GetString((byte*)pMessage, length);
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
            var str = Encoding.ASCII.GetString((byte*)pMessage, length);
            Debug.WriteLine($"{source}:{type}:{id}:{severity}:{str}");
            if (type is DebugType.DebugTypeError)
                throw new InvalidOperationException(str);
        }
#pragma warning restore S1172

        GL.DebugMessageCallback(throwErrors ? OnDebugMessageThrowing : OnDebugMessage, default);
        GL.Enable(EnableCap.DebugOutput);
        InDebug = true;
    }

    /// <summary>
    /// Turns off debug features
    /// </summary>
    public static void DisableDebug()
    {
        GL.DebugMessageCallback(default, default);
        GL.Disable(EnableCap.DebugOutput);
        InDebug = false;
    }

    /// <summary>
    /// Retrieve error code.
    /// </summary>
    public static ErrorCode GetError() =>
        GL.GetError();

    /// <summary>
    /// Throws on error
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ThrowIfError()
    {
        var errorCode = GetError();
        if (errorCode is not ErrorCode.NoError)
            throw new InvalidOperationException(errorCode.ToString());
    }

    /// <summary>
    /// Creates a debug group scope, this is a helper for method call
    /// </summary>
    /// <param name="id"></param>
    /// <param name="source"></param>
    /// <param name="caller"></param>
    /// <returns></returns>
    public static DebugGroupScope DebugGroup(int id = default, DebugSourceExternal source = DebugSourceExternal.DebugSourceApplication, [CallerMemberName] string? caller = default)
    {
        if (!InDebug)
            return default;
        (caller ?? throw new ArgumentNullException(nameof(caller))).EnsureASCII();
        GL.PushDebugGroup(source, id, -1, caller);
        return new DebugGroupScope();
    }

    /// <summary>
    /// Creates a debug group scope, this is a helper for user named scope
    /// </summary>
    /// <param name="message"></param>
    /// <param name="id"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static DebugGroupScope DebugGroup(string message, int id = default, DebugSourceExternal source = DebugSourceExternal.DebugSourceApplication)
    {
        if (!InDebug)
            return default;
        message.EnsureASCII();
        GL.PushDebugGroup(source, id, -1, message);
        return new DebugGroupScope();
    }

    /// <summary>
    /// Debug group scope, dispose once to close, stateless.
    /// </summary>
    public readonly struct DebugGroupScope : IDisposable
    {
        /// <summary>
        /// Just closes the debug group
        /// </summary>
        public void Dispose()
        {
            if (!InDebug)
                return;
            GL.PopDebugGroup();
        }
    }

}
