using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.UI
{
    /// <summary>
    /// Capa de UI para el menú principal: dibuja título, opciones y maneja input.
    /// </summary>
    public class UiMainMenu
    {
        private readonly ISceneManager   _scenes;
        private readonly IAssetLoader    _assets;
        private readonly PersistenceManager _persistence;
        private readonly GraphicsDevice  _graphicsDevice;

        private SpriteFont  _titleFont      = null!;
        private SpriteFont  _optionFont     = null!;
        private Texture2D?  _background;

        private readonly List<string> _options = new() { "New Game", "Load Game", "Options", "Exit" };
        private int                  _selectedIndex;

        private Vector2 _titlePosition;
        private Vector2 _optionOrigin;
        private Vector2 _optionsStart;
        private float   _optionSpacing    = 50f;

        private KeyboardState _prevKeyboardState;
        private double        _inputCooldown    = 0.15;
        private double        _timeSinceLastInput;

        /// <summary>
        /// Crea una instancia de UiMainMenu.
        /// </summary>
        public UiMainMenu(ISceneManager scenes, GraphicsDevice graphicsDevice, IAssetLoader assets, PersistenceManager persistence)
        {
            _scenes        = scenes;
            _graphicsDevice = graphicsDevice;
            _assets        = assets;
            _persistence   = persistence;
        }

        /// <summary>
        /// Carga fuentes, texturas y calcula posiciones.
        /// </summary>
        public void LoadContent()
        {
            _titleFont  = _assets.Load<SpriteFont>("Fonts/MenuFont");
            _optionFont = _assets.Load<SpriteFont>("Fonts/DefaultFont");
            // Fondo opcional (si existe)
            try { _background = _assets.Load<Texture2D>("UI/mainmenu_background"); }
            catch { _background = null; }

            var vp = _graphicsDevice.Viewport;
            // Centrar título
            const string titleText = "The Wonderland";
            var titleSize = _titleFont.MeasureString(titleText);
            _titlePosition = new Vector2((vp.Width - titleSize.X) / 2, vp.Height * 0.1f);

            // Calcular origen para centrar opciones
            float maxWidth = _options.Max(o => _optionFont.MeasureString(o).X);
            _optionOrigin  = new Vector2(maxWidth / 2, 0);
            _optionsStart  = new Vector2(vp.Width / 2, vp.Height * 0.4f);

            _prevKeyboardState = Keyboard.GetState();
        }

        /// <summary>
        /// Navega por las opciones con ↑/↓ y confirma con Enter.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _timeSinceLastInput += gameTime.ElapsedGameTime.TotalSeconds;
            var ks = Keyboard.GetState();

            if (_timeSinceLastInput >= _inputCooldown)
            {
                if (ks.IsKeyDown(Keys.Down) && !_prevKeyboardState.IsKeyDown(Keys.Down))
                {
                    _selectedIndex = (_selectedIndex + 1) % _options.Count;
                    _timeSinceLastInput = 0;
                }
                else if (ks.IsKeyDown(Keys.Up) && !_prevKeyboardState.IsKeyDown(Keys.Up))
                {
                    _selectedIndex = (_selectedIndex - 1 + _options.Count) % _options.Count;
                    _timeSinceLastInput = 0;
                }
                else if (ks.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
                {
                    OnSelect(_selectedIndex);
                    _timeSinceLastInput = 0;
                }
            }

            _prevKeyboardState = ks;
        }

        /// <summary>
        /// Actúa según la opción seleccionada.
        /// </summary>
        private void OnSelect(int index)
        {
            switch (index)
            {
                case 0: // New Game
                    _scenes.ChangeScene("Gameplay");
                    break;
                case 1: // Load Game
                    var data = _persistence.LoadGame();
                    if (data != null)
                    {
                        _scenes.ChangeScene("Gameplay", data);
                    }
                    break;
                case 2: // Options
                    // TODO: empujar escena de opciones
                    break;
                case 3: // Exit
                    Environment.Exit(0);
                    break;
            }
        }

        /// <summary>
        /// Dibuja fondo, título y menú de opciones.
        /// </summary>
        public void Draw(SpriteBatch sb)
        {
            // Fondo
            if (_background != null)
                sb.Draw(_background,
                        new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
                        Color.White);

            // Título
            const string titleText = "The Wonderland";
            sb.DrawString(_titleFont, titleText, _titlePosition, Color.CornflowerBlue);

            // Opciones
            for (int i = 0; i < _options.Count; i++)
            {
                var text  = _options[i];
                var pos   = _optionsStart + new Vector2(0, i * _optionSpacing);
                var color = i == _selectedIndex ? Color.Yellow : Color.White;

                sb.DrawString(_optionFont,
                              text,
                              pos,
                              color,
                              0f,
                              _optionOrigin,
                              1f,
                              SpriteEffects.None,
                              0f);
            }
        }
    }
}
