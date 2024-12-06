namespace OvenTK.Lib;
internal class Shape<TVert,TIndice>(BufferBase verticesHandle, BufferBase indicesHandle, TVert[] vertices, TIndice[] indices)
{
    public TVert[] Vertices => vertices;

    public TIndice[] Indices => indices;

    public BufferBase VerticesHandle { get; } = verticesHandle;

    public BufferBase IndicesHandle { get; } = indicesHandle;
}
