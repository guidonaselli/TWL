using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Client.Presentation.UI;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes;

public sealed class SceneMainMenu : SceneBase
{
    private readonly UiMainMenu _ui;

    public SceneMainMenu(
        ContentManager content,
        GraphicsDevice graphicsDevice,
        ISceneManager scenes,
        IAssetLoader assets,
        NetworkClient networkClient,
        PersistenceManager persistence
    ) : base(content, graphicsDevice, scenes, assets)
    {
        // Inicializo mi UI pasando los servicios necesarios
        _ui = new UiMainMenu(scenes, graphicsDevice, assets, networkClient, persistence);
    }

    public override void Initialize()
    {
        base.Initialize();
        // Si necesitas lógica extra al inicializar, aquí va
    }

    public override void LoadContent()
    {
        base.LoadContent();
        // Cargo la UI (fuentes, fondos, etc)
        _ui.LoadContent();
    }

    public override void Update(
        GameTime gameTime,
        MouseState mouseState,
        KeyboardState keyboardState
    )
    {
        // Delego la entrada al UI
        _ui.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();
        _ui.Draw(spriteBatch);
        spriteBatch.End();
    }
}
