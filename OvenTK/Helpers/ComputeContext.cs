using OvenTK.Lib.Helpers;

namespace OvenTK.Lib;

/// <summary>
/// Compute context scope
/// </summary>
public class ComputeContext : ContextBase
{
    /// <summary>
    /// Shader program to use
    /// </summary>
    public ShaderProgram Program { get; set; }

    /// <summary>
    /// Helper to use correct textures
    /// </summary>
    public TextureArray? TextureArray { get; set; }

    /// <summary>
    /// Creates compute context
    /// </summary>
    /// <param name="program"></param>
    /// <param name="textureArray"></param>
    public ComputeContext(ShaderProgram program, TextureArray? textureArray = default)
    {
        Program = program;
        TextureArray = textureArray;
    }

    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public override Scope Use()
    {
        TextureArray?.Use();
        Program.Use();
        return base.Use();
    }
}
