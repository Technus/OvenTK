using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;

/// <summary>
/// Wrapper for OpenGL Shader Program (a group of shaders)
/// </summary>
[DebuggerDisplay("{Handle}")]
public class ShaderProgram : IDisposable
{
    private bool _disposed;
    private Dictionary<string, (int location, int size, ActiveUniformType type)>? _uniformLocations;
    private Dictionary<string, (int location, int size, ActiveAttribType type)>? _attributeLocations;

    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="handle"></param>
    protected ShaderProgram(int handle) => Handle = handle;

    /// <summary>
    /// Casts to <see cref="Handle"/>
    /// </summary>
    /// <param name="data"></param>
    public static implicit operator int(ShaderProgram? data) => data?.Handle ?? default;
    
    /// <summary>
    /// OpenGL handle
    /// </summary>
    public int Handle { get; protected set; }

    /// <summary>
    /// The access to shaders Uniform metadata
    /// </summary>
    public IReadOnlyDictionary<string, (int location, int size, ActiveUniformType type)> UniformLocations
    {
        get
        {
            if (_uniformLocations is null)
            {
                GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var count);

                _uniformLocations = [];

                for (var i = 0; i < count; i++)
                {
                    // get the name of this uniform,
                    var key = GL.GetActiveUniform(Handle, i, out var size, out var type);

                    // get the location,
                    var location = GL.GetUniformLocation(Handle, key);

                    // and then add it to the dictionary.
                    _uniformLocations.Add(key, (location, size, type));
                }
            }

            return _uniformLocations;
        }
    }

    /// <summary>
    /// The access to shaders Vertex Array Attribute metadata
    /// </summary>
    public IReadOnlyDictionary<string, (int location, int size, ActiveAttribType type)> AttributeLocations
    {
        get
        {
            if (_attributeLocations is null)
            {
                GL.GetProgram(Handle, GetProgramParameterName.ActiveAttributes, out var count);

                _attributeLocations = [];

                for (var i = 0; i < count; i++)
                {
                    // get the name of this uniform,
                    var key = GL.GetActiveAttrib(Handle, i, out var size, out var type);

                    // get the location,
                    var location = GL.GetAttribLocation(Handle, key);

                    // and then add it to the dictionary.
                    _attributeLocations.Add(key, (location, size, type));
                }
            }

            return _attributeLocations;
        }
    }

    /// <summary>
    /// Helper to build a simple vertex+fragment shader program.<br/>
    /// This is how you create a simple shader.
    /// Shaders are written in GLSL, which is a language very similar to C in its semantics.
    /// The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    /// A commented example of GLSL can be found in shader.vert.
    /// </summary>
    /// <param name="vertStream">vertex shader source</param>
    /// <param name="fragStream">fragment shader source</param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static async Task<ShaderProgram> CreateFromAsync(Stream vertStream, Stream fragStream, Encoding encoding = default!)
    {
        using var vert = vertStream;
        using var frag = fragStream;
        encoding ??= Encoding.UTF8;
        using var vertReader = new StreamReader(vert, encoding);
        using var fragReader = new StreamReader(frag, encoding);
        var vertStr = vertReader.ReadToEndAsync();
        var fragStr = fragReader.ReadToEndAsync();
        return CreateFrom(await vertStr, await fragStr);
    }

    /// <summary>
    /// Helper to build a simple vertex+fragment shader program.<br/>
    /// This is how you create a simple shader.
    /// Shaders are written in GLSL, which is a language very similar to C in its semantics.
    /// The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    /// A commented example of GLSL can be found in shader.vert.
    /// </summary>
    /// <param name="vert">vertex shader source</param>
    /// <param name="frag">fragment shader source</param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static ShaderProgram CreateFrom(byte[] vert, byte[] frag, Encoding encoding = default!)
    {
        encoding ??= Encoding.UTF8;
        return CreateFrom(encoding.GetString(vert), encoding.GetString(frag));
    }

    /// <summary>
    /// Helper to build a simple vertex+fragment shader program.<br/>
    /// This is how you create a simple shader.
    /// Shaders are written in GLSL, which is a language very similar to C in its semantics.
    /// The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    /// A commented example of GLSL can be found in shader.vert.
    /// </summary>
    /// <param name="vert">vertex shader source</param>
    /// <param name="frag">fragment shader source</param>
    /// <returns></returns>
    public static ShaderProgram CreateFrom(string vert, string frag)
    {
        using var vertexShader = Shader.CreateFrom(ShaderType.VertexShader, vert);
        using var fragmentShader = Shader.CreateFrom(ShaderType.FragmentShader, frag);
        return CreateFrom([vertexShader, fragmentShader]);
    }

    /// <summary>
    /// Helper to build a shader program.<br/>
    /// This is how you create a simple shader.
    /// Shaders are written in GLSL, which is a language very similar to C in its semantics.
    /// The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    /// A commented example of GLSL can be found in shader.vert.
    /// </summary>
    /// <param name="shaders">shader sources</param>
    /// <returns></returns>
    public static ShaderProgram CreateFrom(IEnumerable<Shader> shaders)
    {
        int handle = GL.CreateProgram();

        foreach (var shader in shaders)
            GL.AttachShader(handle, shader.Handle);

        LinkProgram(handle);

        foreach (var shader in shaders)
            GL.DetachShader(handle, shader.Handle);

        return new ShaderProgram(handle);
    }

    /// <summary>
    /// Links all shaders together to form a program
    /// </summary>
    /// <param name="program"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void LinkProgram(int program)
    {
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetProgramInfoLog(program);
            throw new InvalidOperationException($"Error occurred whilst linking Program({program}).\n\n{infoLog}");
        }
    }

    /// <summary>
    /// Sets this shader program as current<br/>
    /// A wrapper function that enables the shader program.
    /// </summary>
    public void Use() => GL.UseProgram(Handle);

    // Uniform setters
    // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
    // You use VBOs for vertex-related data, and uniforms for almost everything else.

    // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
    //     1. Bind the program you want to set the uniform on
    //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
    //     3. Use the appropriate GL.Uniform* function to set the uniform.

    /// <summary>
    /// Disposes using dispose pattern, deletes the program
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }

            try
            {
                GL.DeleteProgram(Handle);
            }
            catch (AccessViolationException e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                Handle = default;
            }

            _uniformLocations = null;

            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~ShaderProgram() => Dispose(disposing: false);

    /// <summary>
    /// Disposes using dispose pattern, deletes the program
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}