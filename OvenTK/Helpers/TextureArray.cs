namespace OvenTK.Lib.Helpers;
/// <summary>
/// Helper to 'simplify' reapplying textures to units
/// </summary>
public class TextureArray
{
    [ThreadStatic]
    private static Dictionary<int, (TextureBase texture, Action binding)>? _textureCacheOnThread;
    private static Dictionary<int, (TextureBase texture, Action binding)> _textureCache => _textureCacheOnThread ??= [];
    /// <summary>
    /// List of currently loaded textures, as long as the texture array was used
    /// </summary>
    /// <remarks>Backed by ThreadStatic Field to support multiple contexts</remarks>
    public static IReadOnlyDictionary<int, (TextureBase texture, Action binding)> TextureCache => _textureCache;

    /// <summary>
    /// Add textures here then you can use the texture array<br/>
    /// binding action should be a constant operation, in case some details need to be changed replace the entire entry with new instance<br/>
    /// Treat the binding action as shorthand for a struct containing all necessary data to bind the texture
    /// </summary>
    public Dictionary<int, (TextureBase texture, Action binding)> Textures { get; } = [];

    /// <summary>
    /// Applies the current <see cref="Textures"/> state
    /// </summary>
    public void Use()
    {
        foreach (var texture in Textures)
        {
            if(_textureCache.TryGetValue(texture.Key, out var value) && value == texture.Value)
                continue;

            texture.Value.binding();
            _textureCache[texture.Key] = texture.Value;
        }
    }
}
