using OpenTK.Graphics.OpenGL4;

namespace OvenTK.Lib;
public class Shape(int verticesHandle, int indicesHandle, float[] vertices, int[] indices)
{
    public float[] Vertices => vertices;

    public int[] Indices => indices;
}
