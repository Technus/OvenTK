using Mapper;
using StbImageSharp;
using System.ComponentModel;
using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// This sprite sheet uses integer indexing of sprites, for more convinient one use <see cref="SpriteSheet{TKey}"/>
/// </summary>
public class SpriteSheet : IDisposable
{
    internal static readonly double _log2 = Math.Log(2);

    internal readonly SpriteTex[] _sprites;
    internal bool _disposedValue;

    /// <summary>
    /// The sprite sheet texture
    /// </summary>
    public Texture Texture { get; private set; }
    /// <summary>
    /// The buffer holding sprite positions and sizes
    /// </summary>
    public BufferStorage Buffer { get; private set; }
    /// <summary>
    /// The texture buffer holing the buffer
    /// </summary>
    public TextureBuffer TextureBuffer { get; private set; }

    /// <summary>
    /// Use factory method
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="storage"></param>
    /// <param name="buffer"></param>
    /// <param name="sprites"></param>
    public SpriteSheet(Texture texture, BufferStorage storage, TextureBuffer buffer, SpriteTex[] sprites)
    {
        Texture = texture;
        Buffer = storage;
        TextureBuffer = buffer;
        _sprites = sprites;
    }

    /// <summary>
    /// Adds labels to internal components
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public SpriteSheet WithLabel(string label)
    {
        if (!DebugExtensions.InDebug)
            return this;
        label.EnsureASCII();
        GL.ObjectLabel(ObjectLabelIdentifier.Texture, Texture.Handle, -1, $"{label}:Texture");
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Buffer.Handle, -1, $"{label}:BufferStorage");
        GL.ObjectLabel(ObjectLabelIdentifier.Texture, TextureBuffer.Handle, -1, $"{label}:TextureBuffer");
        return this;
    }

    /// <summary>
    /// Creates sprite sheet from sprites based <paramref name="imageList"/><br/>
    /// The <paramref name="imageList"/> count should be equal to or greater than defined consecutive enums (excluding 0 value)
    /// </summary>
    /// <param name="imageList"></param>
    /// <param name="mapper"></param>
    /// <param name="maxMipLevels"></param>
    /// <returns>The sprite sheet with 0 being an empty placeholder and images from list taking next places in the <see cref="RectImage.Id"/> order starting from 1</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static SpriteSheet CreateFrom(List<RectImage> imageList, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        var minSize = int.MaxValue;

        foreach (var image in imageList)
        {
            minSize = Math.Min(minSize, Math.Min(image.Width, image.Height));
        }

        var mipLevels = Math.Min(maxMipLevels, (int)Math.Floor(Math.Log(minSize) / _log2));//this ensures no color bleed and max mipping

        var data = new SpriteTex[imageList.Count + 1];//for null

        mapper ??= new MapperOptimalEfficiency<Mapping>(new Canvas());
        var id = 1;
        var mapping = mapper.Mapping(imageList);
        var mappedImages = mapping.MappedImages.OrderBy(img=>(img.ImageInfo as RectImage)!.Id).Select(img =>
        {
            if (img.ImageInfo is not RectImage rectImg)
                throw new InvalidOperationException("invalid image");
            data[id++] = new((short)img.X, (short)img.Y, (short)rectImg.Width, (short)rectImg.Height);
            return (img.X, img.Y, rectImg.ImageResult);
        });

        var spriteSheet = Texture.CreateFrom(mappedImages, mapping.Width, mapping.Height, mipLevels);

        var buffer = BufferStorage.CreateFrom(data);
        var texBuffer = TextureBuffer.CreateFrom(buffer, SizedInternalFormat.Rgba16i);

        return new(spriteSheet, buffer, texBuffer, data);
    }

    /// <summary>
    /// Creates sprite sheet from sprites based on enumeration of streams in <paramref name="images"/><br/>
    /// The <paramref name="images"/> count should be equal to or greater than defined consecutive enums (excluding 0 value)
    /// </summary>
    /// <param name="images"></param>
    /// <param name="mapper"></param>
    /// <param name="maxMipLevels"></param>
    /// <returns>The sprite sheet with 0 being an empty placeholder and images from list taking next places starting from 1</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static SpriteSheet CreateFrom(IEnumerable<Stream> images, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        var imageList = new List<RectImage>();

        var id = 1;
        foreach (var image in images)
        {
            var pow2RectImage = RectImage.CreatePow2Size(image.LoadImageAndDispose(), id++);
            imageList.Add(pow2RectImage);
        }

        return CreateFrom(imageList, mapper, maxMipLevels);
    }

    /// <summary>
    /// Binds/Loads The sprite texture atlase and sprite data texture buffer on the <paramref name="textureUnit"/> and next ones
    /// </summary>
    /// <param name="textureUnit"></param>
    /// <returns>the next "free" texture unit (<paramref name="textureUnit"/>+2)</returns>
    /// <remarks>Loading order is Texture and then Sprite Data</remarks>
    public int UseBase(int textureUnit)
    {
        Texture.Use(textureUnit++);
        TextureBuffer.Use(textureUnit++);
        return textureUnit;
    }

    /// <summary>
    /// Creates an empty buffer for GPU text, as long as it is empty it is sprite sheet agnostic, but a sprite in it would not be.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="hint"></param>
    /// <returns></returns>
    public static BufferData CreateBuffer(int count, BufferUsageHint hint) =>
        BufferData.Create(Unsafe.SizeOf<Sprite>() * count, hint);

    /// <summary>
    /// Each sprite on screen is an single instance of "2 triangles (or a quad...)", this helps to compute how much instances are to be rendered from <paramref name="data"/>
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int InstanceCount(BufferData data) => data.Size / Unsafe.SizeOf<Sprite>();

    /// <summary>
    /// Makes a <see cref="Sprite"/> array the size of <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Sprite[] MakeArray(BufferData data) => new Sprite[data.Size / Unsafe.SizeOf<Sprite>()];

    /// <summary>
    /// Gets the Texture atlas resolution (not normalized)
    /// </summary>
    /// <returns></returns>
    public Vector2 GetResolution() => new(Texture.Width, Texture.Height);

    /// <summary>
    /// Gets the sprite metadata for reference
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SpriteTex GetSprite(int id) => _sprites[id];

    /// <summary>
    /// Returns collection of all sprites
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<SpriteTex> GetSprites() => _sprites;

    /// <summary>
    /// A wrapper for image, the actual content is not positioned inside
    /// </summary>
    public class RectImage : IImageInfo
    {
        /// <summary>
        /// Wraps <paramref name="texture"/> in a box of size <paramref name="apparentHeight"/> , <paramref name="apparentHeight"/><br/>
        /// Where the box should be at least of same size as texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="id"></param>
        /// <param name="apparentWidth"></param>
        /// <param name="apparentHeight"></param>
        public RectImage(ImageResult texture, int id, int apparentWidth, int apparentHeight)
        {
            ImageResult = texture;
            Id = id;
            Width = apparentWidth;
            Height = apparentHeight;
        }

        /// <summary>
        /// Wraps the image in a matching box
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RectImage Create(ImageResult texture, int id) =>
            new(texture, id, texture.Width, texture.Height);

        /// <summary>
        /// Wraps the image in a smallest box of size 2^n which fits the image
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RectImage CreatePow2Size(ImageResult texture, int id) =>
            new(texture, id,
                (int)Math.Pow(2, Math.Ceiling(Math.Log(texture.Width) / _log2)),
                (int)Math.Pow(2, Math.Ceiling(Math.Log(texture.Height) / _log2)));

        /// <summary>
        /// Wraps the image in a box which is bigger by <paramref name="additionalPixels"/>
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="id"></param>
        /// <param name="additionalPixels"></param>
        /// <returns></returns>
        public static RectImage CreateExpanded(ImageResult texture, int id, int additionalPixels = 1) =>
            new(texture, id, texture.Width + additionalPixels, texture.Height + additionalPixels);

        /// <summary>
        /// The actual image
        /// </summary>
        public ImageResult ImageResult { get; }

        /// <summary>
        /// Id for matching to the Sprite enum
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The apparent width to occupy in sprite sheet
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The apparent height to occupy in sprite sheet
        /// </summary>
        public int Height { get; }
    }

    /// <summary>
    /// Used internally to create rectangle mapping
    /// </summary>
    public class Mapping : ISprite
    {
        private readonly List<IMappedImageInfo> _mappedImages = [];
        private int _width;
        private int _height;

        /// <summary>
        /// Holds the locations of all the individual images within the sprite image.
        /// </summary>
        public IEnumerable<IMappedImageInfo> MappedImages => _mappedImages;

        /// <summary>
        /// Width of the sprite image
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// Height of the sprite image
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// Area of the sprite image
        /// </summary>
        public int Area => _width * _height;

        /// <summary>
        /// Adds a Rectangle to the SpriteInfo, and updates the width and height of the SpriteInfo.
        /// </summary>
        /// <param name="mappedImage"></param>
        public void AddMappedImage(IMappedImageInfo mappedImage)
        {
            _mappedImages.Add(mappedImage);

            var newImage = mappedImage.ImageInfo;

            var highestY = mappedImage.Y + newImage.Height;
            var rightMostX = mappedImage.X + newImage.Width;

            if (_height < highestY)
                _height = highestY;
            if (_width < rightMostX)
                _width = rightMostX;
        }
    }

    /// <summary>
    /// Defines sprite position, angle and <see cref="SpriteTex"/> Id in the sprite data buffer
    /// </summary>
    [DebuggerDisplay("{X} {Y} {Angle} {Id}")]
    public struct Sprite
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
        /// gpu sprite to render
        /// </summary>
        public float Id { get; set; }
    }

    /// <summary>
    /// Defines texturespace of a sprite
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    [DebuggerDisplay("{X} {Y} {W} {H}")]
    public readonly struct SpriteTex(
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
    /// Dispose pattern, deletes the textures and buffers
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            TextureBuffer.Dispose();
            Buffer.Dispose();
            Texture.Dispose();
            _disposedValue = true;
        }
    }

    /// <summary>
    /// Dispose pattern
    /// </summary>
    ~SpriteSheet() => Dispose(disposing: false);

    /// <summary>
    /// Dispose pattern, deletes the textures and buffers
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// <typeparamref name="TKey"/> should be an enum starting from 0 without gaps<br/>
/// where 0 will be "None", max size is int/uint
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class SpriteSheet<TKey> : SpriteSheet where TKey : struct, Enum, IConvertible
{
    /// <summary>
    /// Use factory methods
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="storage"></param>
    /// <param name="buffer"></param>
    /// <param name="sprites"></param>
    public SpriteSheet(Texture texture, BufferStorage storage, TextureBuffer buffer, SpriteTex[] sprites) : base(texture, storage, buffer, sprites)
    {
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="spriteSheet"></param>
    public SpriteSheet(SpriteSheet spriteSheet) : base(spriteSheet.Texture, spriteSheet.Buffer, spriteSheet.TextureBuffer, spriteSheet._sprites)
    {
    }

    /// <summary>
    /// Gets the sprite metadata for reference
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public SpriteTex GetSprite(TKey key) => _sprites[key.ToInt32(provider: default)];

    /// <summary>
    /// Creates sprite sheet from sprites based on enum descriptions<br/>
    /// Using <paramref name="textureResolver"/> to map enum <see cref="DescriptionAttribute"/> to image streams
    /// </summary>
    /// <param name="textureResolver"></param>
    /// <param name="mapper"></param>
    /// <param name="maxMipLevels"></param>
    /// <returns></returns>
    public static SpriteSheet<TKey> CreateFrom(Func<TKey, Stream> textureResolver, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        var values = Enum.GetValues(typeof(TKey)).Cast<TKey>().Except([default]);
        return CreateFrom(values.Select(textureResolver.Invoke), mapper, maxMipLevels);
    }

    /// <summary>
    /// Creates sprite sheet from sprites based on enumeration of streams<br/>
    /// The <paramref name="images"/> count sould be equal to or greater than defined consecutive enums (excluding 0 value)
    /// </summary>
    /// <param name="images"></param>
    /// <param name="mapper"></param>
    /// <param name="maxMipLevels"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static new SpriteSheet<TKey> CreateFrom(IEnumerable<Stream> images, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        using var sheet = SpriteSheet.CreateFrom(images, mapper, maxMipLevels);
        sheet._disposedValue = true;//To prevent GC of resources passed to clone
        return new(sheet);
    }
}