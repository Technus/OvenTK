using System.Diagnostics;
using System.Xml.Serialization;

namespace OvenTK.Lib;

public class BitmapFont : IDisposable
{
    private const int _charDataSize = 4;//X,Y,Rotation,Char ID, 4 numbers is max (4 floats)
    private static readonly XmlSerializer _serializer = new(typeof(Font));

    private bool _disposedValue;
    private readonly Font _font;
    private readonly Texture[] _pages;
    private readonly BufferStorage _buffer;
    private readonly TextureBuffer _texBuffer;
    private readonly Dictionary<char, (ushort id, Char def)> _stringToGpu;
    private readonly (ushort id, Char def) _space;

    public BitmapFont(
        Font font,
        Texture[] pages,
        BufferStorage buffer,
        TextureBuffer texBuffer,
        Dictionary<char, (ushort id, Char def)> stringToGpu)
    {
        _font = font;
        _pages = pages;
        _buffer = buffer;
        _texBuffer = texBuffer;
        _stringToGpu = stringToGpu;
        _space = stringToGpu[' '];
    }
    public BufferData CreateStringsAligned(IList<(float x, float y, float rotation, string text)> strings, int maxLen, BufferUsageHint hint) =>
        CreateStringsAligned(strings, maxLen, strings.Count, hint);
    public void RecreateStringsAligned(BufferData buffer, IList<(float x, float y, float rotation, string text)> strings, int maxLen) =>
        RecreateStringsAligned(buffer, strings, maxLen, strings.Count);


    public BufferData CreateStrings(IEnumerable<(float x, float y, float rotation, string text)> strings, BufferUsageHint hint)
    {
        var data = new float[strings.Sum(x => x.text.Length) * _charDataSize];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize*text.Length), x, y, rotation, text);
            i += text.Length * _charDataSize;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    public BufferData CreateStringsAligned(IEnumerable<(float x, float y, float rotation, string text)> strings, int count, int maxLen, BufferUsageHint hint)
    {
        var data = new float[maxLen * _charDataSize * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize * maxLen), x, y, rotation, text);
            i += maxLen * _charDataSize;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    public BufferData CreateStringsAligned(IEnumerable<(float x, float y, float rotation, IEnumerable<char> text)> strings, int count, int maxLen, BufferUsageHint hint)
    {
        var data = new float[maxLen * _charDataSize * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize * maxLen), x, y, rotation, text);
            i += maxLen * _charDataSize;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    public void RecreateStrings(BufferData buffer, IEnumerable<(float x, float y, float rotation, string text)> strings)
    {
        var data = new float[strings.Sum(x => x.text.Length) * _charDataSize];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize * text.Length), x, y, rotation, text);
            i += text.Length * _charDataSize;
        }

        buffer.Recreate(data);
    }

    public void RecreateStringsAligned(BufferData buffer, IEnumerable<(float x, float y, float rotation, string text)> strings, int count, int maxLen)
    {
        var data = new float[maxLen * _charDataSize * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize * maxLen), x, y, rotation, text);
            i += maxLen * _charDataSize;
        }

        buffer.Recreate(data);
    }

    public void RecreateStringsAligned(BufferData buffer, IEnumerable<(float x, float y, float rotation, IEnumerable<char> text)> strings, int count, int maxLen)
    {
        var data = new float[maxLen * _charDataSize * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLine(data.AsSpan(i, _charDataSize * maxLen), x, y, rotation, text);
            i += maxLen * _charDataSize;
        }

        buffer.Recreate(data);
    }

    private void WriteLine(Span<float> dataOut, float x, float y, float rotation, IEnumerable<char> text)
    {
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);
        var advance = 0;
        var i = 0;
        foreach (var ch in text)
        {
            if (!_stringToGpu.TryGetValue(ch, out var val))
                val = _space;
            var (dx, dy) = (advance + val.def.Xoffset, _font.Common.LineHeight-( val.def.Yoffset+val.def.Height));//pre rotation
            var (rdx, rdy) = (cos * dx - sin * dy, sin * dx + cos * dy);//post rotation
            dataOut[i+0] = (float)(x + rdx);
            dataOut[i+1] = (float)(y + rdy);
            dataOut[i+2] = rotation;
            dataOut[i+3] = val.id;

            advance += val.def.Xadvance;
            i += _charDataSize;
        }
    }

    public static BitmapFont CreateFrom(Stream stream, Func<string, Stream> textureResolver, int mipLevels = Texture._mipDefault)
    {
        if (_serializer.Deserialize(stream) is not Font font)
            throw new InvalidOperationException("Deserialized null");
        stream.Dispose();

        var pages = new Texture[font.Pages.Page.Count];
        for (int page = 0; page < pages.Length; page++)
            pages[page] = Texture.CreateFrom(textureResolver(font.Pages.Page[page].File), mipLevels);

        var stringToGpu = new Dictionary<char, (ushort id, Char def)>();

        var data = new Glyph[font.Chars.Char.Count];
        font.Chars.Char.Sort();

        for (int i = 0; i < data.Length; i++)
        {
            var charDef = font.Chars.Char[i];
            stringToGpu[charDef.Id] = ((ushort)i, charDef);
            data[i] = new(
                charDef.X,
                (short)(pages[charDef.Page].Height - charDef.Y - charDef.Height),
                charDef.Width,
                charDef.Height);
        }

        var buffer = BufferStorage.CreateFrom(data);
        var texBuffer = TextureBuffer.CreateFrom(buffer, SizedInternalFormat.Rgba16i);

        return new(font, pages, buffer, texBuffer, stringToGpu);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _texBuffer.Dispose();
            _buffer.Dispose();

            foreach (var page in _pages)
            {
                page.Dispose();
            }
            _stringToGpu.Clear();
            _disposedValue = true;
        }
    }

    public int InstanceCount(BufferData data) => data.Size / (Unsafe.SizeOf<float>()*_charDataSize);

    public int PageCount() => _font.Pages.Page.Count;

    public int UseBase(int textureUnit)
    {
        foreach (var page in _pages)
            GL.BindTextureUnit(textureUnit++, page);
        GL.BindTextureUnit(textureUnit++, _texBuffer);
        return textureUnit;
    }

    ~BitmapFont() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [DebuggerDisplay("{X1} {Y1} {X2} {Y2} {X2-X1} {Y2-Y1} {Page}")]
    public readonly struct Glyph
    {
        public short X { get; }
        public short Y { get; }
        public short W { get; }
        public short H { get; }

        public Glyph(
            short x, short y,
            short w, short h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }
    }

    [XmlRoot(ElementName = "info")]
    public class Info
    {

        [XmlAttribute(AttributeName = "face")]
        public string Face { get; set; }

        [XmlAttribute(AttributeName = "size")]
        public int Size { get; set; }

        [XmlAttribute(AttributeName = "bold")]
        public int Bold { get; set; }

        [XmlAttribute(AttributeName = "italic")]
        public int Italic { get; set; }

        [XmlAttribute(AttributeName = "charset")]
        public string Charset { get; set; }

        [XmlAttribute(AttributeName = "unicode")]
        public int Unicode { get; set; }

        [XmlAttribute(AttributeName = "stretchH")]
        public int StretchH { get; set; }

        [XmlAttribute(AttributeName = "smooth")]
        public int Smooth { get; set; }

        [XmlAttribute(AttributeName = "aa")]
        public int Aa { get; set; }

        [XmlAttribute(AttributeName = "padding")]
        public string Padding { get; set; }

        [XmlAttribute(AttributeName = "spacing")]
        public string Spacing { get; set; }

        [XmlAttribute(AttributeName = "outline")]
        public int Outline { get; set; }
    }

    [XmlRoot(ElementName = "common")]
    public class Common
    {

        [XmlAttribute(AttributeName = "lineHeight")]
        public int LineHeight { get; set; }

        [XmlAttribute(AttributeName = "base")]
        public int Base { get; set; }

        [XmlAttribute(AttributeName = "scaleW")]
        public int ScaleW { get; set; }

        [XmlAttribute(AttributeName = "scaleH")]
        public int ScaleH { get; set; }

        [XmlAttribute(AttributeName = "pages")]
        public int Pages { get; set; }

        [XmlAttribute(AttributeName = "packed")]
        public int Packed { get; set; }

        [XmlAttribute(AttributeName = "alphaChnl")]
        public int AlphaChnl { get; set; }

        [XmlAttribute(AttributeName = "redChnl")]
        public int RedChnl { get; set; }

        [XmlAttribute(AttributeName = "greenChnl")]
        public int GreenChnl { get; set; }

        [XmlAttribute(AttributeName = "blueChnl")]
        public int BlueChnl { get; set; }
    }

    [XmlRoot(ElementName = "page")]
    public class Page
    {

        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "file")]
        public string File { get; set; }
    }

    [XmlRoot(ElementName = "pages")]
    public class Pages
    {

        [XmlElement(ElementName = "page")]
        public List<Page> Page { get; set; }
    }

    [XmlRoot(ElementName = "char")]
    public class Char : IComparable<Char>
    {

        [XmlAttribute(AttributeName = "id")]
        public char Id { get; set; }

        [XmlAttribute(AttributeName = "x")]
        public short X { get; set; }

        [XmlAttribute(AttributeName = "y")]
        public short Y { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public short Width { get; set; }

        [XmlAttribute(AttributeName = "height")]
        public short Height { get; set; }

        [XmlAttribute(AttributeName = "xoffset")]
        public short Xoffset { get; set; }

        [XmlAttribute(AttributeName = "yoffset")]
        public short Yoffset { get; set; }

        [XmlAttribute(AttributeName = "xadvance")]
        public short Xadvance { get; set; }

        [XmlAttribute(AttributeName = "page")]
        public ushort Page { get; set; }

        [XmlAttribute(AttributeName = "chnl")]
        public sbyte Chnl { get; set; }

        public int CompareTo(Char? other) => Id.CompareTo(other?.Id ?? '\0');
    }

    [XmlRoot(ElementName = "chars")]
    public class Chars
    {

        [XmlElement(ElementName = "char")]
        public List<Char> Char { get; set; }

        [XmlAttribute(AttributeName = "count")]
        public int Count { get; set; }
    }

    [XmlRoot(ElementName = "font")]
    public class Font
    {

        [XmlElement(ElementName = "info")]
        public Info Info { get; set; }

        [XmlElement(ElementName = "common")]
        public Common Common { get; set; }

        [XmlElement(ElementName = "pages")]
        public Pages Pages { get; set; }

        [XmlElement(ElementName = "chars")]
        public Chars Chars { get; set; }
    }
}
