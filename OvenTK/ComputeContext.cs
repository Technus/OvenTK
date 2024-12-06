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
    /// Creates compute context
    /// </summary>
    /// <param name="program"></param>
    public ComputeContext(ShaderProgram program)
    {
        Program = program;
    }

    /// <summary>
    /// Creates drawing scope
    /// </summary>
    /// <returns></returns>
    public override Scope Use()
    {
        Program.Use();
        return base.Use();
    }
}
