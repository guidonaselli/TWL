using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Client.Presentation.Scenes;
using TWL.Shared.Net.Abstractions;
using TWL.Shared.Net.Network;

namespace TWL.Client.Presentation.Core
{
    public sealed class Game1 : Game
    {
        private readonly SceneManager _scenes;
        private readonly GameManager  _gameManager;
        private readonly GameClientManager _gameClientManager;
        private readonly LoopbackChannel _net;
        private readonly Logger<Game1> _log;
        private readonly AssetLoader _assets;
        private readonly PersistenceManager _persistence;
        private readonly GameClientManager _gameClientManager;

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        public Game1(
            SceneManager scenes,
            GameManager  gameManager,
            GameClientManager gameClientManager,
            LoopbackChannel net,
            PersistenceManager persistence,
            Logger<Game1> log)
        {
            _scenes = scenes;
            _gameManager = gameManager;
            _gameClientManager = gameClientManager;
            _net = net;
            _persistence = persistence;
            _log = log;

            // Configuración inicial de MonoGame
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Creamos el loader de assets (se registra internamente GraphicsDevice y Content)
            _assets = new AssetLoader(Services);
        }

        protected override void Initialize()
        {
            // Solo registramos las escenas; no cargamos contenido todavía
            _scenes.RegisterScene("MainMenu",
                new SceneMainMenu(Content, GraphicsDevice, _scenes, _assets, _persistence));
            _scenes.RegisterScene("Gameplay",
                new SceneGameplay(Content, GraphicsDevice, _scenes, _assets, _gameClientManager, _net, _persistence));
            _scenes.RegisterScene("Battle",
                new SceneBattle(Content, GraphicsDevice, _scenes, _assets));
            _scenes.RegisterScene("Marketplace",
                new SceneMarketplace(Content, GraphicsDevice, _scenes, _assets,
                    new MarketplaceManager()));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Aquí GraphicsDevice ya está listo
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Cambiamos a la primera escena: esto llama a LoadContent() de SceneMainMenu
            _scenes.ChangeScene("MainMenu");
        }

        protected override void Update(GameTime gameTime)
        {
            if (_gameManager.IsPaused)
                return;

            _gameManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Todas las escenas dibujan sobre este único SpriteBatch
            _spriteBatch.Begin();
            _scenes.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
