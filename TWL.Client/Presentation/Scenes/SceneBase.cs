using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Map;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes;

/// <summary>
/// Clase base mínima para todas las escenas.
/// Los métodos virtuales están *vacíos a propósito*: muchas escenas
/// no necesitan lógica en ellos y así evitamos que cada sub-clase
/// los implemente con cuerpos vacíos.
/// </summary>
public abstract class SceneBase : IScene
{
    protected readonly Camera2D Camera;
    protected readonly ContentManager Content;
    protected readonly GraphicsDevice GraphicsDevice;
    protected readonly ISceneManager Scenes;
    protected readonly IAssetLoader Assets;

    protected SceneBase(
        ContentManager content,
        GraphicsDevice gd,
        ISceneManager scenes,
        IAssetLoader assets)
    {
        Content = content;
        GraphicsDevice = gd;
        Scenes = scenes;
        Assets = assets;
        Camera = new Camera2D();
    }

    public bool IsInitialized { get; private set; }

    public virtual void Initialize() => IsInitialized = true;
    public virtual void LoadContent() { }
    public virtual void UnloadContent() { }
    public virtual void Update(GameTime time, MouseState mouse, KeyboardState keys) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
    public virtual Camera2D GetCamera() => Camera;
}
