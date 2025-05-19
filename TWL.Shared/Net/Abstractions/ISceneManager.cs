using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TWL.Shared.Net.Abstractions;

/// <summary>Gestor central de escenas.</summary>
public interface ISceneManager
{
    IScene? CurrentScene { get; }
    void RegisterScene(string key, IScene scene);
    void ChangeScene(string key, object? payload = null);
    void PushScene(IScene scene);
    void PopScene();
    void Update(GameTime time, MouseState mouse, KeyboardState keyboard);
    void Draw(SpriteBatch spriteBatch);
}