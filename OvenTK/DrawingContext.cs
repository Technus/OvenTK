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
    public VertexArray VertexArray { get; set; }

    /// <summary>
    /// Creates draw context
    /// </summary>
    /// <param name="program"></param>
    /// <param name="vertexArray"></param>
    public DrawingContext(ShaderProgram program, VertexArray vertexArray)
    {
        Program = program;
        VertexArray = vertexArray;
    }

    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public override Scope Use()
    {
        VertexArray.Use();
        Program.Use();
        return base.Use();
    }
}
