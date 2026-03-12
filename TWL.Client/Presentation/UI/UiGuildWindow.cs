using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.UI;

public class UiGuildWindow : UiWindow
{
    private readonly GameClientManager _clientManager;
    private readonly Networking.NetworkClient _networkClient;
    private SpriteFont _font;
    private Texture2D _pixel;

    // Chat Input State
    private bool _isTyping = false;
    private string _currentInput = "";
    private TimeSpan _cursorBlinkTimer;
    private bool _cursorVisible;
    private Microsoft.Xna.Framework.Input.KeyboardState _lastKeyboardState;
    private bool _showWindow = false;

    public UiGuildWindow(GameClientManager clientManager, Networking.NetworkClient networkClient) : base(new Rectangle(220, 200, 300, 250))
    {
        _clientManager = clientManager;
        _networkClient = networkClient;
        Visible = true;
    }

    public void ToggleVisibility()
    {
        _showWindow = !_showWindow;
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Fonts/DefaultFont");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime, Microsoft.Xna.Framework.Input.MouseState mouse, Microsoft.Xna.Framework.Input.KeyboardState keyboard)
    {
        // Toggle window with 'G' key if not typing
        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G) && _lastKeyboardState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.G))
        {
            if (!_isTyping) ToggleVisibility();
        }

        base.Update(gameTime, mouse, keyboard);

        if (!_showWindow)
        {
            _lastKeyboardState = keyboard;
            return;
        }

        // Toggle typing with Enter
        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) && _lastKeyboardState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            if (_isTyping)
            {
                // Send message
                if (!string.IsNullOrWhiteSpace(_currentInput))
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new GuildChatSendRequest { Message = _currentInput });
                    _networkClient.SendNetMessage(new TWL.Shared.Net.Network.NetMessage
                    {
                        Op = TWL.Shared.Net.Network.Opcode.GuildChatRequest,
                        JsonPayload = payload
                    });
                    _currentInput = "";
                }
                _isTyping = false;
            }
            else
            {
                _isTyping = true;
            }
        }
        else if (_isTyping)
        {
            var keys = keyboard.GetPressedKeys();
            foreach (var key in keys)
            {
                if (!_lastKeyboardState.IsKeyDown(key))
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.Back && _currentInput.Length > 0)
                    {
                        _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                    }
                    else if (key == Microsoft.Xna.Framework.Input.Keys.Space)
                    {
                        _currentInput += " ";
                    }
                    else
                    {
                        var keyString = key.ToString();
                        if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
                        {
                            _currentInput += keyString;
                        }
                    }
                }
            }
        }

        _lastKeyboardState = keyboard;

        if (_isTyping)
        {
            _cursorBlinkTimer += gameTime.ElapsedGameTime;
            if (_cursorBlinkTimer.TotalMilliseconds > 500)
            {
                _cursorVisible = !_cursorVisible;
                _cursorBlinkTimer = TimeSpan.Zero;
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!_showWindow)
        {
            Visible = false;
            return;
        }

        Visible = true;

        // Background
        sb.Draw(_pixel, _bounds, new Color(0, 0, 0, 200));

        // Header
        sb.DrawString(_font, "Guild Roster & Chat", new Vector2(_bounds.X + 5, _bounds.Y + 5), Color.Yellow);

        int yOffset = 25;

        // Render Roster
        var onlineCount = _clientManager.GuildRoster.Count(m => m.IsOnline);
        sb.DrawString(_font, $"Members Online: {onlineCount}/{_clientManager.GuildRoster.Count}", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.LightGray);
        yOffset += 20;

        foreach (var member in _clientManager.GuildRoster.OrderByDescending(m => m.IsOnline).ThenByDescending(m => m.Rank).Take(5))
        {
            var color = member.IsOnline ? Color.Green : Color.Gray;
            string memberText = $"[{member.Rank}] {member.Name} (Lv.{member.Level})";
            sb.DrawString(_font, memberText, new Vector2(_bounds.X + 5, _bounds.Y + yOffset), color);
            yOffset += 15;
        }

        // Chat Overlay
        yOffset = 130;
        sb.DrawString(_font, "--- Guild Chat ---", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.Yellow);
        yOffset += 15;

        var recentChat = _clientManager.GuildChatLogs.Skip(Math.Max(0, _clientManager.GuildChatLogs.Count - 4)).Take(4);
        foreach (var msg in recentChat)
        {
             sb.DrawString(_font, $"[{msg.Timestamp:HH:mm}] {msg.SenderName}: {msg.Message}", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.Cyan);
             yOffset += 15;
        }

        // Input Field
        yOffset += 5;
        if (_isTyping)
        {
            var inputText = _currentInput + (_cursorVisible ? "|" : "");
            sb.DrawString(_font, $"Chat: {inputText}", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.White);
        }
        else
        {
            sb.DrawString(_font, "Press ENTER to chat", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.Gray);
        }
    }
}
