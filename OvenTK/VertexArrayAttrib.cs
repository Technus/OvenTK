using System.Diagnostics;

namespace OvenTK.Lib;

[DebuggerDisplay("{Index}")]
public class VertexArrayAttrib
{
    public int Index { get; protected set; }

    protected VertexArrayAttrib(int index)
    {
        Index = index;
    }

    public static implicit operator int(VertexArrayAttrib data) => data.Index;


}
