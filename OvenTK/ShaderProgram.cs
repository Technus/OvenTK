using System.Diagnostics;
using System.Text;

namespace OvenTK.Lib;

// A simple class meant to help create shaders.

[DebuggerDisplay("{Handle}")]
public class ShaderProgram : IDisposable
{
    private bool _disposed;
    private Dictionary<string, int>? _uniformLocations;

    protected ShaderProgram(int handle)
    {
        Handle = handle;
    }

    public static implicit operator int(ShaderProgram? data) => data?.Handle ?? default;
    
    public int Handle { get; private set; }

    public IReadOnlyDictionary<string, int> UniformLocations
    {
        get
        {
            if (_uniformLocations is null)
            {
                // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
                // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
                // later.

                // First, we have to get the number of active uniforms in the shader.
                GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

                // Next, allocate the dictionary to hold the locations.
                _uniformLocations = [];

                // Loop over all the uniforms,
                for (var i = 0; i < numberOfUniforms; i++)
                {
                    // get the name of this uniform,
                    var key = GL.GetActiveUniform(Handle, i, out _, out _);

                    // get the location,
                    var location = GL.GetUniformLocation(Handle, key);

                    // and then add it to the dictionary.
                    _uniformLocations.Add(key, location);
                }
            }

            return _uniformLocations;
        }
    }

    public static async Task<ShaderProgram> CreateFromAsync(Stream vert, Stream frag, Encoding encoding = default!)
    {
        using var vertReader = new StreamReader(vert, encoding);
        using var fragReader = new StreamReader(frag, encoding);
        var vertStr = vertReader.ReadToEndAsync();
        var fragStr = fragReader.ReadToEndAsync();
        return CreateFrom(await vertStr, await fragStr);
    }

    public static ShaderProgram CreateFrom(byte[] vert, byte[] frag, Encoding encoding = default!)
    {
        encoding ??= Encoding.UTF8;
        return CreateFrom(encoding.GetString(vert), encoding.GetString(frag));
    }

    // This is how you create a simple shader.
    // Shaders are written in GLSL, which is a language very similar to C in its semantics.
    // The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    // A commented example of GLSL can be found in shader.vert.
    public static ShaderProgram CreateFrom(string vertSrc, string fragSrc)
    {
        using var vertexShader = Shader.CreateFrom(ShaderType.VertexShader, vertSrc);
        using var fragmentShader = Shader.CreateFrom(ShaderType.FragmentShader, fragSrc);
        return CreateFrom(vertexShader, fragmentShader);
    }

    public static ShaderProgram CreateFrom(params Shader[] shaders) 
        => CreateFrom(shaders as IEnumerable<Shader>);

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

    private static void LinkProgram(int program)
    {
        // We link the program
        GL.LinkProgram(program);

        // Check for linking errors
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code != (int)All.True)
        {
            // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
            throw new InvalidOperationException($"Error occurred whilst linking Program({program})");
        }
    }

    // A wrapper function that enables the shader program.
    public void Use() => GL.UseProgram(Handle);

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    public int GetAttribLocation(string attribName) => GL.GetAttribLocation(Handle, attribName);

    // Uniform setters
    // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
    // You use VBOs for vertex-related data, and uniforms for almost everything else.

    // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
    //     1. Bind the program you want to set the uniform on
    //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
    //     3. Use the appropriate GL.Uniform* function to set the uniform.

    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetInt(string name, int data) => GL.ProgramUniform1(Handle, UniformLocations[name], data);

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat(string name, float data) => GL.ProgramUniform1(Handle, UniformLocations[name], data);

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix4(string name, Matrix4 data) => GL.ProgramUniformMatrix4(Handle, UniformLocations[name], true, ref data);

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector3(string name, Vector3 data) => GL.ProgramUniform3(Handle, UniformLocations[name], data);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Nothing
            }

            //if (GL.GetInteger(GetPName.CurrentProgram) == Handle)
            //    GL.UseProgram(default);
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

    ~ShaderProgram() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}