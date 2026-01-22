using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TWL.Client.Managers;
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
        private readonly SettingsManager _settings;
        private readonly AssetLoader _assets;
        private readonly PersistenceManager _persistence;

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        public Game1(
            SceneManager scenes,
            GameManager  gameManager,
            GameClientManager gameClientManager,
            LoopbackChannel net,
            SettingsManager settings,
            Logger<Game1> log,
            PersistenceManager persistence)
        {
            _scenes = scenes;
            _gameManager = gameManager;
            _gameClientManager = gameClientManager;
            _net = net;
            _settings = settings;
            _log = log;
            _persistence = persistence;

            // Configuración inicial de MonoGame
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Activated += (s, e) => _settings.SetMuteState(false);
            Deactivated += (s, e) =>
            {
                if (_settings.MuteOnUnfocus)
                    _settings.SetMuteState(true);
            };

            // Creamos el loader de assets (se registra internamente GraphicsDevice y Content)
            _assets = new AssetLoader(Services);
        }

        protected override void Initialize()
        {
            // Cargar datos estáticos
            try
            {
                var skillsJson = System.IO.File.ReadAllText("Content/Data/skills.json");
                TWL.Shared.Domain.Skills.SkillRegistry.Instance.LoadSkills(skillsJson);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error loading skills: {ex.Message}");
            }

            // Solo registramos las escenas; no cargamos contenido todavía
            _scenes.RegisterScene("MainMenu",
                new SceneMainMenu(Content, GraphicsDevice, _scenes, _assets, _gameClientManager.NetworkClient, _persistence));

            _scenes.RegisterScene("Gameplay",
                new SceneGameplay(Content, GraphicsDevice, _scenes, _assets, _gameClientManager, _net, _persistence));

            _scenes.RegisterScene("Battle",
                new SceneBattle(Content, GraphicsDevice, _scenes, _assets));

            _scenes.RegisterScene("Marketplace",
                new SceneMarketplace(Content, GraphicsDevice, _scenes, _assets,
                    new MarketplaceManager()));

            _scenes.RegisterScene("Options",
                new SceneOptions(Content, GraphicsDevice, _scenes, _assets, _settings));

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
