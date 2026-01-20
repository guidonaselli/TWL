using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace TWL.Client.Presentation.Graphics;

public static class PaletteTextureCache
{
    // Key: "AssetPath_ColorHash" -> Texture
    private static readonly Dictionary<string, Texture2D> _cache = new();

    public static Texture2D? Get(string key)
    {
        return _cache.TryGetValue(key, out var tex) ? tex : null;
    }

    public static void Add(string key, Texture2D tex)
    {
        if (!_cache.ContainsKey(key))
        {
            _cache[key] = tex;
        }
    }

    public static void Clear()
    {
        foreach (var tex in _cache.Values)
        {
            tex.Dispose();
        }
        _cache.Clear();
    }
}
