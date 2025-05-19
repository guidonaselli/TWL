using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TWL.Shared.Net.Abstractions;
using Microsoft.Extensions.Logging;

namespace TWL.Client.Presentation.Managers
{
    public sealed class AssetLoader : IAssetLoader, IDisposable
    {
        private readonly ContentManager _content;
        private readonly ILogger<AssetLoader>? _logger;
        private readonly ConcurrentDictionary<string, Lazy<object>> _cache = new();

        public AssetLoader(IServiceProvider services, ILogger<AssetLoader>? logger = null)
        {
            _content = new ContentManager(services, "Content");
            _logger  = logger;
        }

        public T Load<T>(string asset)
        {
            if (string.IsNullOrWhiteSpace(asset))
                throw new ArgumentException("El nombre del asset no puede estar vacío.", nameof(asset));

            var key = $"{typeof(T).FullName}|{asset}";

            try
            {
                var lazy = _cache.GetOrAdd(key,
                    _ => new Lazy<object>(() => LoadAsset<T>(asset), isThreadSafe: true));
                return (T)lazy.Value!;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cargando asset {Asset} de tipo {Type}", asset, typeof(T).Name);
                throw;
            }
        }

        public T LoadAsset<T>(string asset)
        {
            try
            {
                return _content.Load<T>(asset)!;
            }
            catch (ContentLoadException) when (typeof(T) == typeof(SpriteFont))
            {
                _logger?.LogWarning("No se encontró fuente '{Font}', usando DefaultFont.", asset);
                return _content.Load<T>("Fonts/DefaultFont");
            }
        }

        public void UnloadAll()
        {
            _logger?.LogInformation("Descargando todos los assets y limpiando caché.");
            _cache.Clear();
            _content.Unload();
        }

        public void Dispose()
        {
            UnloadAll();
            _content.Dispose();
        }
    }
}
