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
    internal readonly Texture _texture;
    internal readonly BufferStorage _buffer;
    internal readonly TextureBuffer _texBuffer;
    internal readonly SpriteTex[] _sprites;
    internal bool _disposedValue;

    /// <summary>
    /// Use factory method
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="storage"></param>
    /// <param name="buffer"></param>
    /// <param name="sprites"></param>
    public SpriteSheet(Texture texture, BufferStorage storage, TextureBuffer buffer, SpriteTex[] sprites)
    {
        _texture = texture;
        _buffer = storage;
        _texBuffer = buffer;
        _sprites = sprites;
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
    public static SpriteSheet CreateFrom(IEnumerable<Stream> images, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        var imageList = new List<Pow2RectImage>();
        var minSize = int.MaxValue;

        foreach (var image in images)
        {
            var pow2RectImage = new Pow2RectImage(image.LoadImageAndDispose());
            minSize = Math.Min(minSize, Math.Min(pow2RectImage.Width, pow2RectImage.Height));
            imageList.Add(pow2RectImage);
        }

        var mipLevels = Math.Min(maxMipLevels, (int)Math.Floor(Math.Log(minSize) / Extensions._log2));//this ensures no color bleed and max mipping

        var data = new SpriteTex[imageList.Count + 1];//for null
        var i = 1;

        mapper ??= new MapperOptimalEfficiency<Mapping>(new Canvas());
        var mapping = mapper.Mapping(imageList);
        var mappedImages = mapping.MappedImages.Select(img =>
        {
            if (img.ImageInfo is not Pow2RectImage rectImg)
                throw new InvalidOperationException("invalid image");
            data[i++] = new((short)img.X, (short)img.Y, (short)rectImg.Width, (short)rectImg.Height);
            return (img.X, img.Y, rectImg.ImageResult);
        });

        var spriteSheet = Texture.CreateFrom(mappedImages, mapping.Width, mapping.Height, mipLevels);

        var buffer = BufferStorage.CreateFrom(data);
        var texBuffer = TextureBuffer.CreateFrom(buffer, SizedInternalFormat.Rgba16i);

        return new(spriteSheet, buffer, texBuffer, data);
    }

    /// <summary>
    /// Binds/Loads The sprite texture atlase and sprite data texture buffer on the <paramref name="textureUnit"/> and next ones
    /// </summary>
    /// <param name="textureUnit"></param>
    /// <returns>the next "free" texture unit (<paramref name="textureUnit"/>+2)</returns>
    /// <remarks>Loading order is Texture and then Sprite Data</remarks>
    public int UseBase(int textureUnit)
    {
        _texture.Use(textureUnit++);
        _texBuffer.Use(textureUnit++);
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
    public Vector2 GetResolution() => new(_texture.Width, _texture.Height);

    /// <summary>
    /// Gets the sprite metadata for reference
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SpriteTex GetSprite(int id) => _sprites[id];

    internal sealed class Pow2RectImage : IImageInfo
    {
        public Pow2RectImage(ImageResult texture)
        {
            ImageResult = texture;
            Width = (int)Math.Pow(2, Math.Ceiling(Math.Log(ImageResult.Width) / Extensions._log2));
            Height = (int)Math.Pow(2, Math.Ceiling(Math.Log(ImageResult.Height) / Extensions._log2));
        }

        public ImageResult ImageResult { get; }

        public int Width { get; }

        public int Height { get; }
    }

    /// <summary>
    /// Used internally to create rectangle mapping
    /// </summary>
    public class Mapping : ISprite
    {
        private readonly List<IMappedImageInfo> _mappedImages = [];
        private int _width = 0;
        private int _height = 0;

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

            _texBuffer.Dispose();
            _buffer.Dispose();
            _texture.Dispose();
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
    public SpriteSheet(SpriteSheet spriteSheet) : base(spriteSheet._texture, spriteSheet._buffer, spriteSheet._texBuffer, spriteSheet._sprites)
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