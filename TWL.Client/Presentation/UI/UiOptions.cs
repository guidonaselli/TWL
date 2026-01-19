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
    /// UI layer for the Options menu.
    /// </summary>
    public class UiOptions
    {
        private readonly ISceneManager   _scenes;
        private readonly IAssetLoader    _assets;
        private readonly GraphicsDevice  _graphicsDevice;

        private SpriteFont  _titleFont      = null!;
        private SpriteFont  _optionFont     = null!;
        private Texture2D?  _background;

        private readonly List<string> _options = new() { "Sound: On", "Music: On", "Back" };
        private int                  _selectedIndex;

        private Vector2 _titlePosition;
        private Vector2 _optionOrigin;
        private Vector2 _optionsStart;
        private float   _optionSpacing    = 50f;

        private KeyboardState _prevKeyboardState;
        private double        _inputCooldown    = 0.15;
        private double        _timeSinceLastInput;

        public UiOptions(ISceneManager scenes, GraphicsDevice graphicsDevice, IAssetLoader assets)
        {
            _scenes        = scenes;
            _graphicsDevice = graphicsDevice;
            _assets        = assets;
        }

        public void LoadContent()
        {
            _titleFont  = _assets.Load<SpriteFont>("Fonts/MenuFont");
            _optionFont = _assets.Load<SpriteFont>("Fonts/DefaultFont");
            // Reuse main menu background if available
            try { _background = _assets.Load<Texture2D>("UI/mainmenu_background"); }
            catch { _background = null; }

            var vp = _graphicsDevice.Viewport;
            // Center title
            const string titleText = "Options";
            var titleSize = _titleFont.MeasureString(titleText);
            _titlePosition = new Vector2((vp.Width - titleSize.X) / 2, vp.Height * 0.1f);

            // Calculate origin to center options
            CalculateLayout(vp);

            _prevKeyboardState = Keyboard.GetState();
        }

        private void CalculateLayout(Viewport vp)
        {
             float maxWidth = 0f;
             if (_options.Count > 0)
             {
                 maxWidth = _options.Max(o => _optionFont.MeasureString(o).X);
             }

             _optionOrigin  = new Vector2(maxWidth / 2, 0);
             _optionsStart  = new Vector2(vp.Width / 2, vp.Height * 0.4f);
        }

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
                else if (ks.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape))
                {
                     // Escape also goes back
                    _scenes.ChangeScene("MainMenu");
                    _timeSinceLastInput = 0;
                }
            }

            _prevKeyboardState = ks;
        }

        private void OnSelect(int index)
        {
            switch (index)
            {
                case 0: // Sound
                    // Toggle Sound (mock)
                    _options[0] = _options[0] == "Sound: On" ? "Sound: Off" : "Sound: On";
                    CalculateLayout(_graphicsDevice.Viewport); // Recalculate if width changes significantly
                    break;
                case 1: // Music
                    // Toggle Music (mock)
                    _options[1] = _options[1] == "Music: On" ? "Music: Off" : "Music: On";
                     CalculateLayout(_graphicsDevice.Viewport);
                    break;
                case 2: // Back
                    _scenes.ChangeScene("MainMenu");
                    break;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            // Background
            if (_background != null)
                sb.Draw(_background,
                        new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
                        Color.White);

            // Title
            const string titleText = "Options";
            sb.DrawString(_titleFont, titleText, _titlePosition, Color.CornflowerBlue);

            // Options
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
