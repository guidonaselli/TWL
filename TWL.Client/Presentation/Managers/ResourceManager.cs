using System;
using Microsoft.Xna.Framework.Content;
using TWL.Client.Presentation.Core;

namespace TWL.Client.Presentation.Managers;

public sealed class ResourceManager : Singleton<ResourceManager>
{
    private ContentManager? _content;
    private IServiceProvider? _services;

    public void SetServices(IServiceProvider services)
    {
        _services = services;
        _content  = new ContentManager(services, "Content");
    }

    public void SetContentManager(ContentManager cm) => _content = cm;


    public T Load<T>(string asset) where T : class
    {
        if (_services != null)
            return _content is null
                ? throw new InvalidOperationException("ContentManager not set")
                : new AssetLoader(_services).Load<T>(asset);
        if (_content is null)
            throw new InvalidOperationException("ContentManager not set");
        return _content.Load<T>(asset);
    }
}