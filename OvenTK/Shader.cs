using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;

[DebuggerDisplay("{Handle}")]
public readonly struct Shader(int handle) : IDisposable
{
    public int Handle => handle;

    public static implicit operator int(Shader shader) => shader.Handle;

    public void Dispose() => GL.DeleteShader(handle);

    public static async Task<Shader> CreateFromAsync(ShaderType type, Stream shader, Encoding encoding = default!)
    {
        using var stream = shader;
        using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8);
        return CreateFrom(type, await streamReader.ReadToEndAsync());
    }

    public static Shader CreateFrom(ShaderType type, Stream shader, Encoding encoding = default!)
    {
        using var stream = shader;
        using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8);
        return CreateFrom(type, streamReader.ReadToEnd());
    }

    public static Shader CreateFrom(ShaderType type, byte[] shader, Encoding encoding = default!)
        => CreateFrom(type, (encoding ?? Encoding.UTF8).GetString(shader));

    public static Shader CreateFrom(ShaderType type, string shader)
    {
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