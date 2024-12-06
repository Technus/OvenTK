using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;

/// <summary>
/// Wrapper for any OpenGl Shader
/// </summary>
/// <param name="handle"></param>
[DebuggerDisplay("{Handle}")]
public readonly struct Shader(int handle) : IDisposable
{
    /// <summary>
    /// Shader OpenGl Handle
    /// </summary>
    public int Handle => handle;

    /// <summary>
    /// Casting to <see cref="Handle"/>
    /// </summary>
    /// <param name="shader"></param>
    public static implicit operator int(Shader shader) => shader.Handle;

    /// <summary>
    /// Adds a label to this object
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public Shader WithLabel(string label)
    {
        if (!Extensions._isDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Shader, Handle, -1, label);
        return this;
    }

    /// <summary>
    /// Dispose by removing from OpenGl, since it is a struct this will not be called on garbage collection?
    /// </summary>
    public void Dispose() => GL.DeleteShader(handle);

    /// <summary>
    /// Create shader of a certain <paramref name="type"/> using the <paramref name="shader"/> stream as source
    /// </summary>
    /// <param name="type"></param>
    /// <param name="shader">shader source should not contain non ASCII chars</param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <remarks><paramref name="shader"/> will be closed and disposed</remarks>
    public static async Task<Shader> CreateFromAsync(ShaderType type, Stream shader, Encoding encoding = default!)
    {
        using var stream = shader;
        using var streamReader = new StreamReader(stream, encoding ?? Encoding.ASCII);
        return CreateFrom(type, await streamReader.ReadToEndAsync());
    }

    /// <summary>
    /// Create shader of a certain <paramref name="type"/> using the <paramref name="shader"/> stream as source
    /// </summary>
    /// <param name="type"></param>
    /// <param name="shader">shader source should not contain non ASCII chars</param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <remarks><paramref name="shader"/> will be closed and disposed</remarks>
    public static Shader CreateFrom(ShaderType type, Stream shader, Encoding encoding = default!)
    {
        using var stream = shader;
        using var streamReader = new StreamReader(stream, encoding ?? Encoding.ASCII);
        return CreateFrom(type, streamReader.ReadToEnd());
    }

    /// <summary>
    /// Create shader of a certain <paramref name="type"/> using the <paramref name="shader"/> stream as source
    /// </summary>
    /// <param name="type"></param>
    /// <param name="shader">shader source should not contain non ASCII chars</param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <remarks><paramref name="shader"/> will be closed and disposed</remarks>
    public static Shader CreateFrom(ShaderType type, byte[] shader, Encoding encoding = default!)
        => CreateFrom(type, (encoding ?? Encoding.ASCII).GetString(shader));

    /// <summary>
    /// Create shader of a certain <paramref name="type"/> using the <paramref name="shader"/> stream as source
    /// </summary>
    /// <param name="type"></param>
    /// <param name="shader">shader source should not contain non ASCII chars</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <remarks><paramref name="shader"/> will be closed and disposed</remarks>
    public static Shader CreateFrom(ShaderType type, string shader)
    {
        shader.EnsureASCII();
        var handle = GL.CreateShader(type);
        GL.ShaderSource(handle, shader);

        GL.CompileShader(handle);

        GL.GetShader(handle, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetShaderInfoLog(handle);
            throw new InvalidOperationException($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
        }
        return new(handle);
    }
}