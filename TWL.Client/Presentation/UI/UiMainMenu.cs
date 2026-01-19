using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Helpers;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.UI
{
    public class UiMainMenu
    {
        private enum MenuState { ShowingMenu, ShowingLogin }
        private enum ActiveField { Username, Password, None }

        private readonly ISceneManager _scenes;
        private readonly IAssetLoader _assets;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly NetworkClient _networkClient;
        private readonly PersistenceManager _persistence;

        private SpriteFont _titleFont = null!;
        private SpriteFont _optionFont = null!;
        private Texture2D? _background;

        // --- Estado del Menú Principal ---
        private readonly List<string> _options = new() { "Login", "Load Game", "Exit" };
        private int _mainMenuSelectedIndex;

        // --- Estado del Formulario de Login ---
        private string _username = "";
        private string _password = "";
        private ActiveField _activeField = ActiveField.Username;
        private readonly List<string> _loginOptions = new() { "Login", "Back" };
        private int _loginSelectedIndex = 0;


        private MenuState _currentState = MenuState.ShowingMenu;

        private Vector2 _titlePosition;
        private Vector2 _optionsStart;
        private float _optionSpacing = 50f;

        private KeyboardState _prevKeyboardState;

        public UiMainMenu(
            ISceneManager scenes,
            GraphicsDevice graphicsDevice,
            IAssetLoader assets,
            NetworkClient networkClient,
            PersistenceManager persistence)
        {
            _scenes = scenes;
            _graphicsDevice = graphicsDevice;
            _assets = assets;
            _networkClient = networkClient;
            _persistence = persistence;
        }

        public void LoadContent()
        {
            _titleFont = _assets.Load<SpriteFont>("Fonts/MenuFont");
            _optionFont = _assets.Load<SpriteFont>("Fonts/DefaultFont");
            try { _background = _assets.Load<Texture2D>("UI/mainmenu_background"); }
            catch { _background = null; }

            var vp = _graphicsDevice.Viewport;
            const string titleText = "The Wonderland";
            var titleSize = _titleFont.MeasureString(titleText);
            _titlePosition = new Vector2((vp.Width - titleSize.X) / 2, vp.Height * 0.1f);
            _optionsStart = new Vector2(vp.Width / 2, vp.Height * 0.4f);

            _prevKeyboardState = Keyboard.GetState();
        }

        public void Update(GameTime gameTime)
        {
            var ks = Keyboard.GetState();

            switch (_currentState)
            {
                case MenuState.ShowingMenu:
                    UpdateMainMenu(ks);
                    break;
                case MenuState.ShowingLogin:
                    UpdateLogin(ks);
                    break;
            }

            _prevKeyboardState = ks;
        }

        private void UpdateMainMenu(KeyboardState ks)
        {
            if (ks.IsKeyDown(Keys.Down) && !_prevKeyboardState.IsKeyDown(Keys.Down))
            {
                _mainMenuSelectedIndex = (_mainMenuSelectedIndex + 1) % _options.Count;
            }
            else if (ks.IsKeyDown(Keys.Up) && !_prevKeyboardState.IsKeyDown(Keys.Up))
            {
                _mainMenuSelectedIndex = (_mainMenuSelectedIndex - 1 + _options.Count) % _options.Count;
            }
            else if (ks.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
            {
                OnMainMenuSelect(_mainMenuSelectedIndex);
            }
        }

        private void UpdateLogin(KeyboardState ks)
        {
            if (ks.IsKeyDown(Keys.Tab) && !_prevKeyboardState.IsKeyDown(Keys.Tab))
            {
                if (_activeField == ActiveField.Username) _activeField = ActiveField.Password;
                else _activeField = ActiveField.Username;
            }
            else if (ks.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
            {
                 OnLoginSelected();
            }
            else
            {
                ProcessTextInput(ks);
            }
        }

        private void OnMainMenuSelect(int index)
        {
            switch (index)
            {
                case 0: // Login
                    _currentState = MenuState.ShowingLogin;
                    break;
                case 1: // Load Game
                    if (_persistence.SaveExists())
                    {
                        var data = _persistence.LoadGame();
                        if (data != null)
                        {
                            _scenes.ChangeScene("Gameplay", data);
                        }
                    }
                    else
                    {
                        // Optional: Show "No save game found" feedback
                        Console.WriteLine("No save game found.");
                    }
                    break;
                case 2: // Exit
                    Environment.Exit(0);
                    break;
            }
        }

        private void OnLoginSelected()
        {
            var hashedPassword = CryptographyHelper.HashPassword(_password);

            // TODO: Step 4 - Send login request
            Console.WriteLine($"Attempting login with U: {_username} P_hash: {hashedPassword}");
        }

        private void ProcessTextInput(KeyboardState ks)
        {
            foreach (var key in ks.GetPressedKeys())
            {
                if (!_prevKeyboardState.IsKeyDown(key))
                {
                    char character = GetCharFromKey(key, ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift));
                    if (character != '\0')
                    {
                        if (_activeField == ActiveField.Username) _username += character;
                        else if (_activeField == ActiveField.Password) _password += character;
                    }
                    else if (key == Keys.Back && (_activeField == ActiveField.Username ? _username.Length > 0 : _password.Length > 0))
                    {
                        if (_activeField == ActiveField.Username) _username = _username.Substring(0, _username.Length - 1);
                        else _password = _password.Substring(0, _password.Length - 1);
                    }
                }
            }
        }

        private char GetCharFromKey(Keys key, bool shift)
        {
            if (key >= Keys.A && key <= Keys.Z)
                return (char)(key - Keys.A + (shift ? 'A' : 'a'));
            if (key >= Keys.D0 && key <= Keys.D9)
                return (char)(key - Keys.D0 + (shift ? GetShiftedNumberChar(key) : (char)key));
            // Add more character mappings as needed
            return '\0';
        }

        private char GetShiftedNumberChar(Keys key)
        {
            switch (key)
            {
                case Keys.D1: return '!';
                case Keys.D2: return '@';
                case Keys.D3: return '#';
                case Keys.D4: return '$';
                case Keys.D5: return '%';
                case Keys.D6: return '^';
                case Keys.D7: return '&';
                case Keys.D8: return '*';
                case Keys.D9: return '(';
                case Keys.D0: return ')';
                default: return '\0';
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (_background != null)
                sb.Draw(_background, new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height), Color.White);

            const string titleText = "The Wonderland";
            sb.DrawString(_titleFont, titleText, _titlePosition, Color.CornflowerBlue);

            switch (_currentState)
            {
                case MenuState.ShowingMenu:
                    DrawMainMenu(sb);
                    break;
                case MenuState.ShowingLogin:
                    DrawLogin(sb);
                    break;
            }
        }

        private void DrawMainMenu(SpriteBatch sb)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                var text = _options[i];
                var pos = _optionsStart + new Vector2(0, i * _optionSpacing);
                var color = i == _mainMenuSelectedIndex ? Color.Yellow : Color.White;
                var origin = _optionFont.MeasureString(text) / 2;
                sb.DrawString(_optionFont, text, pos, color, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        private void DrawLogin(SpriteBatch sb)
        {
            float yPos = _optionsStart.Y;

            // Username
            string userText = "Username: " + _username + (_activeField == ActiveField.Username ? "_" : "");
            Vector2 userPos = new Vector2(_graphicsDevice.Viewport.Width / 2 - _optionFont.MeasureString(userText).X / 2, yPos);
            sb.DrawString(_optionFont, userText, userPos, Color.White);
            yPos += _optionSpacing;

            // Password
            string passText = "Password: " + new string('*', _password.Length) + (_activeField == ActiveField.Password ? "_" : "");
            Vector2 passPos = new Vector2(_graphicsDevice.Viewport.Width / 2 - _optionFont.MeasureString(passText).X / 2, yPos);
            sb.DrawString(_optionFont, passText, passPos, Color.White);
            yPos += _optionSpacing * 1.5f;

            // Login/Back Buttons
             for (int i = 0; i < _loginOptions.Count; i++)
             {
                 var text = _loginOptions[i];
                 var pos = new Vector2(_graphicsDevice.Viewport.Width / 2, yPos + i * _optionSpacing);
                 var color = i == _loginSelectedIndex ? Color.Yellow : Color.White;
                 var origin = _optionFont.MeasureString(text) / 2;
                 sb.DrawString(_optionFont, text, pos, color, 0f, origin, 1f, SpriteEffects.None, 0f);
             }
        }
    }
}
