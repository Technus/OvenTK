using OvenTK.Lib.Helpers;

namespace OvenTK.Lib;

/// <summary>
/// Drawing context scope
/// </summary>
public class DrawingContext : ContextBase
{
    /// <summary>
    /// Shader program to use
    /// </summary>
    public ShaderProgram Program { get; set; }

    /// <summary>
    /// Vertex array to use
    /// </summary>
    public VertexArray? VertexArray { get; set; }

    /// <summary>
    /// Helper to use correct textures
    /// </summary>
    public TextureArray? TextureArray { get; set; }

    /// <summary>
    /// Creates draw context
    /// </summary>
    /// <param name="program"></param>
    /// <param name="vertexArray"></param>
    /// <param name="textureArray"></param>
    public DrawingContext(ShaderProgram program, VertexArray? vertexArray = default, TextureArray? textureArray = default)
    {
        Program = program;
        VertexArray = vertexArray;
        TextureArray = textureArray;
    }

    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public override Scope Use()
    {
        VertexArray?.Use();
        TextureArray?.Use();
        Program.Use();
        return base.Use();
    }
}
