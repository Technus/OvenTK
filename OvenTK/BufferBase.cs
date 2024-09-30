namespace OvenTK.Lib;

public abstract class BufferBase
{
    public int Handle { get; protected set; }
    public int Size { get; protected set; }

    public abstract void Resize(int size);

    public static implicit operator int(BufferBase? data) => data?.Handle ?? 0;
}
