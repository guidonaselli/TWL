using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;
using TWL.Client.Presentation.Helpers;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Client.Presentation.Services;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net;
using TWL.Shared.Net.Abstractions;
using TWL.Shared.Net.Payloads;
using TWL.Shared.Net.Network;

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
        private readonly List<string> _options = new() { Loc.T("UI_Login"), Loc.T("UI_Exit") };
        private int _mainMenuSelectedIndex;

        // --- Estado del Formulario de Login ---
        private string _username = "";
        private string _password = "";
        private ActiveField _activeField = ActiveField.Username;
        private readonly List<string> _loginOptions = new() { Loc.T("UI_Login"), Loc.T("UI_Back") };
        private int _loginSelectedIndex = 0;
        private string _loginStatusMessage = "";
        private Color _loginStatusColor = Color.White;
        private bool _netSubscribed;


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
            var titleText = Loc.T("UI_Title");
            var titleSize = _titleFont.MeasureString(titleText);
            _titlePosition = new Vector2((vp.Width - titleSize.X) / 2, vp.Height * 0.1f);
            _optionsStart = new Vector2(vp.Width / 2, vp.Height * 0.4f);

            _prevKeyboardState = Keyboard.GetState();

            if (!_netSubscribed)
            {
                EventBus.Subscribe<NetMessage>(OnNetMessage);
                _netSubscribed = true;
            }

            if (!_networkClient.IsConnected)
            {
                _loginStatusMessage = "Connecting...";
                _loginStatusColor = Color.White;
                try
                {
                    _networkClient.Connect();
                    _loginStatusMessage = "Connected.";
                    _loginStatusColor = Color.LightGreen;
                }
                catch (Exception ex)
                {
                    _loginStatusMessage = $"Connection failed: {ex.Message}";
                    _loginStatusColor = Color.OrangeRed;
                }
            }
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
                case 1: // Exit
                    Environment.Exit(0);
                    break;
            }
        }

        private void OnLoginSelected()
        {
            var username = _username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(_password))
            {
                _loginStatusMessage = "Username and password are required.";
                _loginStatusColor = Color.OrangeRed;
                return;
            }
            if (username.Length > 50)
            {
                _loginStatusMessage = "Username is too long.";
                _loginStatusColor = Color.OrangeRed;
                return;
            }

            var loginRequest = new LoginDTO
            {
                Username = username,
                PassHash = CryptographyHelper.HashPassword(_password)
            };
            var jsonPayload = JsonSerializer.Serialize(loginRequest);
            var netMessage = new NetMessage { Op = Opcode.LoginRequest, JsonPayload = jsonPayload };

            _networkClient.SendNetMessage(netMessage);
            _password = string.Empty;
            _activeField = ActiveField.Username;
            _loginStatusMessage = "Logging in...";
            _loginStatusColor = Color.White;

            Console.WriteLine($"Attempting login for user: {_username}");
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

            var titleText = Loc.T("UI_Title");
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

        private void OnNetMessage(NetMessage msg)
        {
            if (msg.Op != Opcode.LoginResponse) return;
            if (string.IsNullOrWhiteSpace(msg.JsonPayload)) return;

            LoginResponseDto? response = null;
            try
            {
                response = JsonSerializer.Deserialize<LoginResponseDto>(msg.JsonPayload);
            }
            catch (JsonException)
            {
                _loginStatusMessage = "Login failed.";
                _loginStatusColor = Color.OrangeRed;
                return;
            }

            if (response == null || !response.Success)
            {
                _loginStatusMessage = response?.ErrorMessage ?? "Login failed.";
                _loginStatusColor = Color.OrangeRed;
                return;
            }

            _loginStatusMessage = "Login successful.";
            _loginStatusColor = Color.LightGreen;
            _scenes.ChangeScene("Gameplay");
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
            string userText = Loc.T("UI_Username") + _username + (_activeField == ActiveField.Username ? "_" : "");
            Vector2 userPos = new Vector2(_graphicsDevice.Viewport.Width / 2 - _optionFont.MeasureString(userText).X / 2, yPos);
            sb.DrawString(_optionFont, userText, userPos, Color.White);
            yPos += _optionSpacing;

            // Password
            string passText = Loc.T("UI_Password") + new string('*', _password.Length) + (_activeField == ActiveField.Password ? "_" : "");
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

            if (!string.IsNullOrEmpty(_loginStatusMessage))
            {
                var statusPos = new Vector2(_graphicsDevice.Viewport.Width / 2, yPos + _loginOptions.Count * _optionSpacing);
                var statusOrigin = _optionFont.MeasureString(_loginStatusMessage) / 2;
                sb.DrawString(_optionFont, _loginStatusMessage, statusPos, _loginStatusColor, 0f, statusOrigin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
