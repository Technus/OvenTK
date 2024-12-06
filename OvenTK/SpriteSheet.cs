using Mapper;
using StbImageSharp;
using System.Diagnostics;

namespace OvenTK.Lib;

/// <summary>
/// <typeparamref name="TKey"/> should be an enum starting from 0 without gaps<br/>
/// where 0 will be "None", max size is int/uint
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class SpriteSheet<TKey> : IDisposable where TKey : struct, Enum, IConvertible
{
    private readonly Texture _texture;
    private readonly BufferStorage _buffer;
    private readonly TextureBuffer _texBuffer;
    private readonly Sprite[] _sprites;
    private bool _disposedValue;

    /// <summary>
    /// Use factory method
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="storage"></param>
    /// <param name="buffer"></param>
    /// <param name="sprites"></param>
    public SpriteSheet(Texture texture, BufferStorage storage, TextureBuffer buffer, Sprite[] sprites)
    {
        _texture = texture;
        _buffer = storage;
        _texBuffer = buffer;
        _sprites = sprites;
    }

    public static SpriteSheet<TKey> CreateFrom(IEnumerable<Stream> images, IMapper<Mapping>? mapper = default, int maxMipLevels = Texture._mipDefault)
    {
        var imageList = new List<Pow2RectImage>();
        var minSize = int.MaxValue;

        foreach (var image in images)
        {
            var pow2RectImage = new Pow2RectImage(image.LoadImage());
            minSize = Math.Min(minSize, Math.Min(pow2RectImage.Width, pow2RectImage.Height));
            imageList.Add(pow2RectImage);
        }

        var mipLevels = Math.Min(maxMipLevels, (int)Math.Floor(Math.Log(minSize) / Extensions._log2));//this ensures no color bleed and max mipping

        var data = new Sprite[imageList.Count];
        var i = 0;

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

    public int UseBase(int textureUnit)
    {
        GL.BindTextureUnit(textureUnit++, _texture);
        GL.BindTextureUnit(textureUnit++, _texBuffer);
        return textureUnit;
    }

    public Sprite GetSprite(int id) => _sprites[id];

    public Sprite GetSprite(TKey key) => _sprites[key.ToInt32(provider: default)];

    private sealed class Pow2RectImage : IImageInfo
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
    /// Defines texturespace of a glyph
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    [DebuggerDisplay("{X} {Y} {W} {H}")]
    public readonly struct Sprite(
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
            _texture.Dispose();
            _disposedValue = true;
        }
    }

    ~SpriteSheet() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}