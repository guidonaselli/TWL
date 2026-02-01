using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Managers;

/// <summary>Implementación por defecto del gestor de escenas.</summary>
public sealed class SceneManager : ISceneManager
{
    private readonly Dictionary<string, IScene> _catalog = new();
    private readonly Stack<IScene> _stack = new();

    public IScene? CurrentScene => _stack.Any() ? _stack.Peek() : null;

    /* ---------------- registro / cambio ----------------------------- */

    public void RegisterScene(string key, IScene scene) => _catalog[key] = scene;

    public void ChangeScene(string key, object? payload = null)
    {
        if (!_catalog.TryGetValue(key, out var next))
            return;

        while (_stack.TryPop(out var old))
            old.UnloadContent();

        PushScene(next, payload);
    }

    public void PushScene(IScene scene) => PushScene(scene, null);
    public void PopScene()
    {
        if (_stack.TryPop(out var old))
            old.UnloadContent();
    }

    public void Update(GameTime time)
    {
        throw new System.NotImplementedException();
    }

    /* ---------------- MonoGame loop delegation ---------------------- */

    public void Update(GameTime time, MouseState mouse, KeyboardState keyboard)
    {
        CurrentScene?.Update(time,
            mouse,
            keyboard);
    }

    public void Draw(SpriteBatch spriteBatch) => CurrentScene?.Draw(spriteBatch);

    /* ---------------- interno --------------------------------------- */

    private void PushScene(IScene scene, object? payload)
    {
        _stack.Push(scene);

        if (!scene.IsInitialized)
            scene.Initialize();

        if (payload is not null && scene is IPayloadReceiver pr)
            pr.ReceivePayload(payload);

        scene.LoadContent();
    }
}