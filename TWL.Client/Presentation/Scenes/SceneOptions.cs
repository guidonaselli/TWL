using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.UI;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes
{
    public sealed class SceneOptions : SceneBase
    {
        private readonly UiOptions _ui;

        public SceneOptions(
            ContentManager content,
            GraphicsDevice  graphicsDevice,
            ISceneManager   scenes,
            IAssetLoader    assets
        ) : base(content, graphicsDevice, scenes, assets)
        {
            _ui = new UiOptions(scenes, graphicsDevice, assets);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _ui.LoadContent();
        }

        public override void Update(
            GameTime     gameTime,
            MouseState   mouseState,
            KeyboardState keyboardState
        )
        {
            _ui.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _ui.Draw(spriteBatch);
        }
    }
}
