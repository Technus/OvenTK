using System.Diagnostics;
using System.Xml.Serialization;

namespace OvenTK.Lib;

/// <summary>
/// Helper class for perglyph rendering of text.<br/>
/// Requires shader program to render on screen.<br/>
/// Loads XML font from bmfont generator https://www.angelcode.com/products/bmfont/.<br/><br/>
/// 
/// First load the font: <see cref="CreateFrom(Stream, Func{string, Stream}, int)"/><br/>
/// Then make a buffer: <see cref="CreateBufferAligned(int, int, BufferUsageHint)"/> or any other CreateStrings* methods.<br/>
/// Then load everything into GPU (The buffer and the font with <see cref="UseBase(int)"/>), with a matching shader to render on screen.<br/><br/>
/// 
/// Buffer layout uses <see cref="Char"/><br/>
/// While GPU Buffers are textures, and texture buffer with layout <see cref="CharTex"/>
/// 
/// </summary>
/// <remarks>No support for embedded new lines or for multiple pages as of now.</remarks>
public class BitmapFont : IDisposable
{
    private static readonly XmlSerializer _serializer = new(typeof(Font));

    private bool _disposedValue;
    private readonly Font _font;
    private readonly Texture[] _pages;
    private readonly BufferStorage _buffer;
    private readonly TextureBuffer _texBuffer;
    private readonly Dictionary<char, (ushort id, CharDef def)> _stringToGpu;
    private readonly (ushort id, CharDef def) _space;

    /// <summary>
    /// Constructor for the data structure, use <see cref="CreateFrom(Stream, Func{string, Stream}, int)"/>.
    /// </summary>
    /// <param name="font">deserialized font XML</param>
    /// <param name="pages">GPU font textures</param>
    /// <param name="buffer">font data buffer</param>
    /// <param name="texBuffer">font data texture buffer for the font data buffer</param>
    /// <param name="stringToGpu">char to GPU char mapping</param>
    public BitmapFont(
        Font font,
        Texture[] pages,
        BufferStorage buffer,
        TextureBuffer texBuffer,
        Dictionary<char, (ushort id, CharDef def)> stringToGpu)
    {
        _font = font;
        _pages = pages;
        _buffer = buffer;
        _texBuffer = texBuffer;
        _stringToGpu = stringToGpu;
        _space = stringToGpu[' '];
    }

    /// <summary>
    /// Factory method to load bitmap font
    /// </summary>
    /// <param name="fontDefinition">Stream to XML file of the font definition</param>
    /// <param name="textureResolver">fuction to resolve textures from texture names</param>
    /// <param name="mipLevels">texture mip levels</param>
    /// <returns>Bitmap font from <paramref name="fontDefinition"/></returns>
    /// <exception cref="InvalidOperationException">When font couldn't be deserialized correctly</exception>
    public static BitmapFont CreateFrom(Stream fontDefinition, Func<string, Stream> textureResolver, int mipLevels = Texture._mipDefault)
    {
        //Load texture definition
        using var stream = fontDefinition;
        if (_serializer.Deserialize(stream) is not Font font || 
            font.Chars is null || font.Chars.Char is null ||
            font.Pages is null || font.Pages.Page is null ||
            font.Common is null)
            throw new InvalidOperationException("Deserialized null");

        if (font.Chars.Char.Count != font.Chars.Count)
            throw new InvalidOperationException("Char count missmatch");
        if (font.Pages.Page.Count != font.Common.Pages)
            throw new InvalidOperationException("Page count missmatch");

        font.Chars.Char.Sort();
        font.Pages.Page.Sort();

        //Load texures
        var pages = new Texture[font.Pages.Page.Count];
        for (int page = 0; page < pages.Length; page++)
            pages[page] = Texture.CreateFrom(textureResolver(font.Pages.Page[page].File!), mipLevels);

        //Map char to GPU glyph
        var charCount = font.Chars.Char.Count;
        var data = new CharTex[charCount + 1];//+1 for ensuring null mapping
        var stringToGpu = new Dictionary<char, (ushort id, CharDef def)>();

        for (int i = 0; i < charCount; i++)
        {
            var charDef = font.Chars.Char[i];
            var gpuId = (ushort)(i + 1);

            stringToGpu[charDef.Id] = (gpuId, charDef);

            data[gpuId] = new(
                charDef.X,
                (short)(pages[charDef.Page].Height - charDef.Y - charDef.Height),//Y flip...
                charDef.Width,
                charDef.Height);
        }

        //Map null to spacebar
        stringToGpu['\0'] = stringToGpu[' '];

        //Create buffer for font data
        var buffer = BufferStorage.CreateFrom(data);
        var texBuffer = TextureBuffer.CreateFrom(buffer, SizedInternalFormat.Rgba16i);

        return new(font, pages, buffer, texBuffer, stringToGpu);
    }

    /// <summary>
    /// Creates an empty buffer for GPU text, as long as it is empty it is font agnostic, but a text in it would not be.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="maxLen"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static BufferData CreateBufferAligned(int count, int maxLen, BufferUsageHint hint) =>
        BufferData.Create(Unsafe.SizeOf<Char>() * maxLen * count, hint);

    /// <summary>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/>
    /// </summary>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public BufferData CreateStringsFrom(IEnumerable<(float x, float y, float rotation, string text)> strings, BufferUsageHint hint)
    {
        var data = new Char[strings.Sum(x => x.text.Length)];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineTo(data.AsSpan(i, text.Length), x, y, rotation, text.AsSpan());
            i += text.Length;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    /// <summary>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars
    /// </summary>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="maxLen"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public BufferData CreateStringsAlignedFrom(IList<(float x, float y, float rotation, string text)> strings, int maxLen, BufferUsageHint hint) =>
        CreateStringsAlignedFrom(strings, strings.Count, maxLen, hint);

    /// <summary>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars<br/>
    /// limiting string count to <paramref name="count"/>
    /// </summary>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="count"></param>
    /// <param name="maxLen"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public BufferData CreateStringsAlignedFrom(IEnumerable<(float x, float y, float rotation, string text)> strings, int count, int maxLen, BufferUsageHint hint)
    {
        var data = new Char[maxLen * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineTo(data.AsSpan(i, maxLen), x, y, rotation, text.AsSpan());
            i += maxLen;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    /// <summary>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars<br/>
    /// limiting string count to <paramref name="count"/>
    /// </summary>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="count"></param>
    /// <param name="maxLen"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public BufferData CreateStringsAlignedFrom(IEnumerable<(float x, float y, float rotation, IEnumerable<char> text)> strings, int count, int maxLen, BufferUsageHint hint)
    {
        var data = new Char[maxLen * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineEnumerableTo(data.AsSpan(i, maxLen), x, y, rotation, text);
            i += maxLen;
        }

        var buffer = BufferData.CreateFrom(data, hint);
        return buffer;
    }

    /// <summary>
    /// Recreates the buffer to point to new data<br/>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/>
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    public void RecreateStringsFrom(BufferData buffer, IEnumerable<(float x, float y, float rotation, string text)> strings)
    {
        var data = new Char[strings.Sum(x => x.text.Length)];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineTo(data.AsSpan(i, text.Length), x, y, rotation, text.AsSpan());
            i += text.Length;
        }

        buffer.Recreate(data);
    }

    /// <summary>
    /// Recreates the buffer to point to new data<br/>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="maxLen"></param>
    public void RecreateStringsAlignedFrom(BufferData buffer, IList<(float x, float y, float rotation, string text)> strings, int maxLen) =>
        RecreateStringsAlignedFrom(buffer, strings, strings.Count, maxLen);

    /// <summary>
    /// Recreates the buffer to point to new data<br/>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars<br/>
    /// limiting string count to <paramref name="count"/>
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="count"></param>
    /// <param name="maxLen"></param>
    public void RecreateStringsAlignedFrom(BufferData buffer, IEnumerable<(float x, float y, float rotation, string text)> strings, int count, int maxLen)
    {
        var data = new Char[maxLen * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineTo(data.AsSpan(i, maxLen), x, y, rotation, text.AsSpan());
            i += maxLen;
        }

        buffer.Recreate(data);
    }

    /// <summary>
    /// Recreates the buffer to point to new data<br/>
    /// Writes this font (family) specific buffer based on <paramref name="strings"/><br/>
    /// aligning the string starting points in the buffer to <paramref name="maxLen"/> chars<br/>
    /// limiting string count to <paramref name="count"/>
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="strings">strings with first baseline point position and rotation</param>
    /// <param name="count"></param>
    /// <param name="maxLen"></param>
    public void RecreateStringsAlignedFrom(BufferData buffer, IEnumerable<(float x, float y, float rotation, IEnumerable<char> text)> strings, int count, int maxLen)
    {
        var data = new Char[maxLen * count];

        var i = 0;
        foreach (var (x, y, rotation, text) in strings)
        {
            WriteLineEnumerableTo(data.AsSpan(i, maxLen), x, y, rotation, text);
            i += maxLen;
        }

        buffer.Recreate(data);
    }

    /// <summary>
    /// Writes the <paramref name="text"/> to <paramref name="dataOut"/>.
    /// </summary>
    /// <param name="dataOut">fragment of the GPU text buffer</param>
    /// <param name="x">baseline position</param>
    /// <param name="y">baseline position</param>
    /// <param name="rotation">text rotation</param>
    /// <param name="text"></param>
    /// <param name="stride">in case the dataOut is transposed stride can be used to iterate over bigger iterator instead</param>
    /// <param name="cleanRest">fill remaining span (also using stride) to '\0' at -inf,-inf position</param>
    public void WriteLineTo(Span<Char> dataOut, float x, float y, float rotation, ReadOnlySpan<char> text, int stride = 1, bool cleanRest = true)
    {
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);
        var advance = 0;
        var i = 0;
        foreach (var ch in text)
        {
            if (!_stringToGpu.TryGetValue(ch, out var val))
                val = _space;
            var (dx, dy) = (advance + val.def.Xoffset, _font.Common!.Base - (val.def.Yoffset + val.def.Height));//pre rotation, with y flip...
            var (rdx, rdy) = (cos * dx - sin * dy, sin * dx + cos * dy);//post rotation
            dataOut[i].X = (float)(x + rdx);
            dataOut[i].Y = (float)(y + rdy);
            dataOut[i].Angle = rotation;
            dataOut[i].Id = val.id;

            advance += val.def.Xadvance;
            i += stride;
        }
        
        if (cleanRest) 
            while(i<dataOut.Length)
            {
                dataOut[i].X = float.NegativeInfinity;
                dataOut[i].Y = float.NegativeInfinity;
                dataOut[i].Angle = 0;
                dataOut[i].Id = 0;
                i += stride;
            }
    }

    /// <summary>
    /// Writes the <paramref name="text"/> to <paramref name="dataOut"/>.
    /// </summary>
    /// <param name="dataOut">fragment of the GPU text buffer</param>
    /// <param name="x">baseline position</param>
    /// <param name="y">baseline position</param>
    /// <param name="rotation">text rotation</param>
    /// <param name="text"></param>
    /// <param name="stride">in case the dataOut is transposed stride can be used to iterate over bigger iterator instead</param>
    /// <param name="cleanRest">fill remaining span (also using stride) to '\0' at -inf,-inf position</param>
    public void WriteLineEnumerableTo(Span<Char> dataOut, float x, float y, float rotation, IEnumerable<char> text, int stride = 1, bool cleanRest = true)
    {
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);
        var advance = 0;
        var i = 0;
        foreach (var ch in text)
        {
            if (!_stringToGpu.TryGetValue(ch, out var val))
                val = _space;
            var (dx, dy) = (advance + val.def.Xoffset, _font.Common!.Base-( val.def.Yoffset+val.def.Height));//pre rotation, with y flip...
            var (rdx, rdy) = (cos * dx - sin * dy, sin * dx + cos * dy);//post rotation
            dataOut[i].X = (float)(x + rdx);
            dataOut[i].Y = (float)(y + rdy);
            dataOut[i].Angle = rotation;
            dataOut[i].Id = val.id;

            advance += val.def.Xadvance;
            i += stride;
        }

        if (cleanRest)
            while (i < dataOut.Length)
            {
                dataOut[i].X = float.NegativeInfinity;
                dataOut[i].Y = float.NegativeInfinity;
                dataOut[i].Angle = 0;
                dataOut[i].Id = 0;
                i += stride;
            }
    }

    /// <summary>
    /// Each glyph on screen is an single instance of "2 triangles (or a quad...)", this helps to compute how much instances are to be rendered from <paramref name="data"/>
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int InstanceCount(BufferData data) => data.Size / Unsafe.SizeOf<Char>();

    /// <summary>
    /// Makes a <see cref="Char"/> array the size of <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Char[] MakeArray(BufferData data) => new Char[data.Size / Unsafe.SizeOf<Char>()];

    /// <summary>
    /// Helper method to judge how many pages were loaded<br/>
    /// </summary>
    /// <returns></returns>
    public int PageCount() => _font.Pages!.Page!.Count;

    public Vector2 GetPageResolution(int page = default) => new(_pages[page].Width, _pages[page].Height);

    /// <summary>
    /// Binds/Loads The font texture atlases and font data texture buffer on the <paramref name="textureUnit"/> and next ones
    /// </summary>
    /// <param name="textureUnit"></param>
    /// <returns>the next "free" texture unit (<paramref name="textureUnit"/>+<see cref="PageCount"/>+1)</returns>
    /// <remarks>Loading order is Pages and then Font Data</remarks>
    public int UseBase(int textureUnit)
    {
        foreach (var page in _pages)
            page.Use(textureUnit++);
        _texBuffer.Use(textureUnit++);
        return textureUnit;
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    /// <param name="disposing"></param>
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


    ~BitmapFont() => Dispose(disposing: false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Defines glyph position, angle and <see cref="CharTex"/> Id in the font data buffer
    /// </summary>
    [DebuggerDisplay("{X} {Y} {Angle} {Id}")]
    public struct Char
    {
        /// <summary>
        /// x pos on screen
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// y pos on screen
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// angle
        /// </summary>
        public float Angle { get; set; }
        /// <summary>
        /// gpu char to render
        /// </summary>
        public float Id { get; set; }
    }

    /// <summary>
    /// Defines texturespace of a glyph
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    [DebuggerDisplay("{X} {Y} {W} {H}")]
    public readonly struct CharTex(
        short x, short y,
        short w, short h)
    {
        /// <summary>
        /// x pos in texture
        /// </summary>
        public short X { get; } = x;
        /// <summary>
        /// y pos in texture
        /// </summary>
        public short Y { get; } = y;
        /// <summary>
        /// width in texture
        /// </summary>
        public short W { get; } = w;
        /// <summary>
        /// height in texture
        /// </summary>
        public short H { get; } = h;
    }

    /// <summary>
    /// Holder for some common info as well as <see cref="Common"/>
    /// </summary>
    [XmlRoot(ElementName = "info")]
    public class Info
    {
        /// <summary>
        /// Name of the font
        /// </summary>
        [XmlAttribute(AttributeName = "face")]
        public string? Face { get; set; }

        /// <summary>
        /// font size in px
        /// </summary>
        [XmlAttribute(AttributeName = "size")]
        public int Size { get; set; }

        /// <summary>
        /// bold if not 0
        /// </summary>
        [XmlAttribute(AttributeName = "bold")]
        public int Bold { get; set; }

        /// <summary>
        /// italic if not 0
        /// </summary>
        [XmlAttribute(AttributeName = "italic")]
        public int Italic { get; set; }

        /// <summary>
        /// charset or empty if unknown or unicode
        /// </summary>
        [XmlAttribute(AttributeName = "charset")]
        public string? Charset { get; set; }

        /// <summary>
        /// unicode if not 0
        /// </summary>
        [XmlAttribute(AttributeName = "unicode")]
        public int Unicode { get; set; }

        /// <summary>
        /// Usually 100 ?
        /// </summary>
        [XmlAttribute(AttributeName = "stretchH")]
        public int StretchH { get; set; }

        /// <summary>
        /// Used supersampling if not 0 ?
        /// </summary>
        [XmlAttribute(AttributeName = "smooth")]
        public int Smooth { get; set; }

        /// <summary>
        /// Anti Aliasing level applied
        /// </summary>
        [XmlAttribute(AttributeName = "aa")]
        public int Aa { get; set; }

        /// <summary>
        /// "#,#,#,#" addtional padding on sides of glyphs
        /// </summary>
        [XmlAttribute(AttributeName = "padding")]
        public string? Padding { get; set; }

        /// <summary>
        /// "#,#" spacing between glyphs
        /// </summary>
        [XmlAttribute(AttributeName = "spacing")]
        public string? Spacing { get; set; }

        /// <summary>
        /// outlines if not 0 ?
        /// </summary>
        [XmlAttribute(AttributeName = "outline")]
        public int Outline { get; set; }
    }

    /// <summary>
    /// Holder for some common info as well as <see cref="Info"/>
    /// </summary>
    [XmlRoot(ElementName = "common")]
    public class Common
    {
        /// <summary>
        /// Space between lines
        /// </summary>
        [XmlAttribute(AttributeName = "lineHeight")]
        public int LineHeight { get; set; }

        /// <summary>
        /// Baseline, as distance from line above
        /// </summary>
        [XmlAttribute(AttributeName = "base")]
        public int Base { get; set; }

        /// <summary>
        /// scaling factor == to texture size
        /// </summary>
        [XmlAttribute(AttributeName = "scaleW")]
        public int ScaleW { get; set; }

        /// <summary>
        /// scaling factor == to texture size
        /// </summary>
        [XmlAttribute(AttributeName = "scaleH")]
        public int ScaleH { get; set; }

        /// <summary>
        /// Count of pages
        /// </summary>
        [XmlAttribute(AttributeName = "pages")]
        public int Pages { get; set; }

        /// <summary>
        /// does it use channel packing
        /// </summary>
        [XmlAttribute(AttributeName = "packed")]
        public int Packed { get; set; }

        /// <summary>
        /// alpha channel id (bit number)
        /// </summary>
        [XmlAttribute(AttributeName = "alphaChnl")]
        public int AlphaChnl { get; set; }

        /// <summary>
        /// red channel id (bit number)
        /// </summary>
        [XmlAttribute(AttributeName = "redChnl")]
        public int RedChnl { get; set; }

        /// <summary>
        /// green channel id (bit number)
        /// </summary>
        [XmlAttribute(AttributeName = "greenChnl")]
        public int GreenChnl { get; set; }

        /// <summary>
        /// blue channel id (bit number)
        /// </summary>
        [XmlAttribute(AttributeName = "blueChnl")]
        public int BlueChnl { get; set; }
    }

    /// <summary>
    /// Holder for font texture definition
    /// </summary>
    [XmlRoot(ElementName = "page")]
    public class Page : IComparable<CharDef>
    {
        /// <summary>
        /// id of the page
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// By default is is relative path from the XML definition
        /// </summary>
        [XmlAttribute(AttributeName = "file")]
        public string? File { get; set; }

        /// <summary>
        /// Compares by <see cref="Id"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(CharDef? other) => Id.CompareTo(other?.Id ?? default);
    }

    /// <summary>
    /// <see cref="BitmapFont.Page"/> array
    /// </summary>
    [XmlRoot(ElementName = "pages")]
    public class Pages
    {
        /// <summary>
        /// List of pages...
        /// </summary>
        [XmlElement(ElementName = "page")]
        public List<Page>? Page { get; set; }
    }

    /// <summary>
    /// Holder for glyph definition
    /// </summary>
    [XmlRoot(ElementName = "char")]
    public class CharDef : IComparable<CharDef>
    {
        /// <summary>
        /// id of the char
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public char Id { get; set; }

        /// <summary>
        /// Texture position X
        /// </summary>
        [XmlAttribute(AttributeName = "x")]
        public short X { get; set; }

        /// <summary>
        /// Texture postion Y
        /// </summary>
        [XmlAttribute(AttributeName = "y")]
        public short Y { get; set; }

        /// <summary>
        /// Texture width
        /// </summary>
        [XmlAttribute(AttributeName = "width")]
        public short Width { get; set; }

        /// <summary>
        /// Texture height
        /// </summary>
        [XmlAttribute(AttributeName = "height")]
        public short Height { get; set; }

        /// <summary>
        /// Whitespace offset X
        /// </summary>
        [XmlAttribute(AttributeName = "xoffset")]
        public short Xoffset { get; set; }

        /// <summary>
        /// Whitespace offset Y
        /// </summary>
        [XmlAttribute(AttributeName = "yoffset")]
        public short Yoffset { get; set; }

        /// <summary>
        /// Advance the cursor after writing
        /// </summary>
        [XmlAttribute(AttributeName = "xadvance")]
        public short Xadvance { get; set; }

        /// <summary>
        /// Page id where the char is located
        /// </summary>
        [XmlAttribute(AttributeName = "page")]
        public ushort Page { get; set; }

        /// <summary>
        /// Channel mask where the char is located as seen in <see cref="Common"/>
        /// </summary>
        [XmlAttribute(AttributeName = "chnl")]
        public sbyte Chnl { get; set; }

        /// <summary>
        /// Compares by <see cref="Id"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(CharDef? other) => Id.CompareTo(other?.Id ?? default);
    }

    /// <summary>
    /// <see cref="BitmapFont.CharDef"/> array
    /// </summary>
    [XmlRoot(ElementName = "chars")]
    public class Chars
    {
        /// <summary>
        /// List of chars...
        /// </summary>
        [XmlElement(ElementName = "char")]
        public List<CharDef>? Char { get; set; }

        /// <summary>
        /// Count of chars
        /// </summary>
        [XmlAttribute(AttributeName = "count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Holder for font definition
    /// </summary>
    [XmlRoot(ElementName = "font")]
    public class Font
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlElement(ElementName = "info")]
        public Info? Info { get; set; }

        [XmlElement(ElementName = "common")]
        public Common? Common { get; set; }

        [XmlElement(ElementName = "pages")]
        public Pages? Pages { get; set; }

        [XmlElement(ElementName = "chars")]
        public Chars? Chars { get; set; }
    }
}
