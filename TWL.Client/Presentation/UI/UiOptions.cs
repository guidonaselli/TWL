using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.UI
{
    public class UiOptions
    {
        private readonly ISceneManager   _scenes;
        private readonly IAssetLoader    _assets;
        private readonly GraphicsDevice  _graphicsDevice;
        private readonly SettingsManager _settings;

        private SpriteFont  _titleFont      = null!;
        private SpriteFont  _optionFont     = null!;
        private Texture2D?  _background;

        private enum OptionType { MasterVolume, MusicVolume, SfxVolume, TextSpeed, MuteOnUnfocus, Back }
        private readonly List<OptionType> _menuItems = new()
        {
            OptionType.MasterVolume,
            OptionType.MusicVolume,
            OptionType.SfxVolume,
            OptionType.TextSpeed,
            OptionType.MuteOnUnfocus,
            OptionType.Back
        };

        private int _selectedIndex;

        private Vector2 _titlePosition;
        private Vector2 _optionsStart;
        private float   _optionSpacing    = 50f;

        private KeyboardState _prevKeyboardState;
        private double        _inputCooldown    = 0.1;
        private double        _timeSinceLastInput;

        public UiOptions(ISceneManager scenes, GraphicsDevice graphicsDevice, IAssetLoader assets, SettingsManager settings)
        {
            _scenes        = scenes;
            _graphicsDevice = graphicsDevice;
            _assets        = assets;
            _settings      = settings;
        }

        public void LoadContent()
        {
            _titleFont  = _assets.Load<SpriteFont>("Fonts/MenuFont");
            _optionFont = _assets.Load<SpriteFont>("Fonts/DefaultFont");
            try { _background = _assets.Load<Texture2D>("UI/mainmenu_background"); }
            catch { _background = null; }

            var vp = _graphicsDevice.Viewport;
            const string titleText = "Options";
            var titleSize = _titleFont.MeasureString(titleText);
            _titlePosition = new Vector2((vp.Width - titleSize.X) / 2, vp.Height * 0.1f);

            _optionsStart  = new Vector2(vp.Width / 2, vp.Height * 0.3f);

            _prevKeyboardState = Keyboard.GetState();
        }

        public void Update(GameTime gameTime)
        {
            _timeSinceLastInput += gameTime.ElapsedGameTime.TotalSeconds;
            var ks = Keyboard.GetState();

            // Always allow navigation if cooldown passed
            if (_timeSinceLastInput >= _inputCooldown)
            {
                bool inputProcessed = false;

                // Vertical Navigation
                if (ks.IsKeyDown(Keys.Down) && _prevKeyboardState.IsKeyUp(Keys.Down))
                {
                    _selectedIndex = (_selectedIndex + 1) % _menuItems.Count;
                    inputProcessed = true;
                }
                else if (ks.IsKeyDown(Keys.Up) && _prevKeyboardState.IsKeyUp(Keys.Up))
                {
                    _selectedIndex = (_selectedIndex - 1 + _menuItems.Count) % _menuItems.Count;
                    inputProcessed = true;
                }

                // Horizontal Adjustment (Settings)
                if (!inputProcessed)
                {
                    if (ks.IsKeyDown(Keys.Left))
                    {
                        AdjustSetting(-1, ks);
                        inputProcessed = true;
                    }
                    else if (ks.IsKeyDown(Keys.Right))
                    {
                        AdjustSetting(1, ks);
                        inputProcessed = true;
                    }
                }

                // Selection / Back
                if (!inputProcessed)
                {
                    if (ks.IsKeyDown(Keys.Enter) && _prevKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        var item = _menuItems[_selectedIndex];
                        if (item == OptionType.Back)
                        {
                            _scenes.ChangeScene("MainMenu");
                        }
                        else if (item == OptionType.MuteOnUnfocus)
                        {
                            _settings.MuteOnUnfocus = !_settings.MuteOnUnfocus;
                        }
                        inputProcessed = true;
                    }
                    else if (ks.IsKeyDown(Keys.Escape) && _prevKeyboardState.IsKeyUp(Keys.Escape))
                    {
                        _scenes.ChangeScene("MainMenu");
                        inputProcessed = true;
                    }
                }

                if (inputProcessed)
                {
                    _timeSinceLastInput = 0;
                }
            }

            _prevKeyboardState = ks;
        }

        private void AdjustSetting(int direction, KeyboardState currentKs)
        {
            var item = _menuItems[_selectedIndex];
            float deltaVol = 0.05f * direction;

            switch (item)
            {
                case OptionType.MasterVolume:
                    _settings.MasterVolume = Math.Clamp(_settings.MasterVolume + deltaVol, 0f, 1f);
                    _settings.ApplyAudioSettings();
                    break;
                case OptionType.MusicVolume:
                    _settings.MusicVolume = Math.Clamp(_settings.MusicVolume + deltaVol, 0f, 1f);
                    _settings.ApplyAudioSettings();
                    break;
                case OptionType.SfxVolume:
                    _settings.SfxVolume = Math.Clamp(_settings.SfxVolume + deltaVol, 0f, 1f);
                    _settings.ApplyAudioSettings();
                    break;
                case OptionType.TextSpeed:
                    // Require fresh press for discrete options to avoid zooming through them
                    if (_prevKeyboardState.IsKeyUp(direction > 0 ? Keys.Right : Keys.Left))
                    {
                        _settings.TextSpeed = Math.Clamp(_settings.TextSpeed + direction, 0, 2);
                    }
                    break;
                case OptionType.MuteOnUnfocus:
                    if (_prevKeyboardState.IsKeyUp(direction > 0 ? Keys.Right : Keys.Left))
                    {
                        _settings.MuteOnUnfocus = !_settings.MuteOnUnfocus;
                    }
                    break;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (_background != null)
                sb.Draw(_background,
                        new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
                        Color.White);

            const string titleText = "Options";
            sb.DrawString(_titleFont, titleText, _titlePosition, Color.CornflowerBlue);

            for (int i = 0; i < _menuItems.Count; i++)
            {
                var item = _menuItems[i];
                string text = GetOptionText(item);

                var size = _optionFont.MeasureString(text);
                var origin = new Vector2(size.X / 2, 0);
                var pos   = _optionsStart + new Vector2(0, i * _optionSpacing);
                var color = i == _selectedIndex ? Color.Yellow : Color.White;

                sb.DrawString(_optionFont, text, pos, color, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        private string GetOptionText(OptionType item)
        {
            return item switch
            {
                OptionType.MasterVolume => $"Master Volume: {(_settings.MasterVolume * 100):0}%",
                OptionType.MusicVolume  => $"Music Volume: {(_settings.MusicVolume * 100):0}%",
                OptionType.SfxVolume    => $"SFX Volume: {(_settings.SfxVolume * 100):0}%",
                OptionType.TextSpeed    => $"Text Speed: {GetTextSpeedLabel(_settings.TextSpeed)}",
                OptionType.MuteOnUnfocus=> $"Mute on Unfocus: {(_settings.MuteOnUnfocus ? "On" : "Off")}",
                OptionType.Back         => "Back",
                _ => ""
            };
        }

        private string GetTextSpeedLabel(int speed) => speed switch { 0 => "Slow", 1 => "Normal", 2 => "Fast", _ => "Normal" };
    }
}
