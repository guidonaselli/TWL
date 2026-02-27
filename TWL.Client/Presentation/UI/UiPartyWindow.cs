using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.State;

namespace TWL.Client.Presentation.UI;

public class UiPartyWindow : UiWindow
{
    private readonly PartyState _partyState;
    private readonly Networking.NetworkClient _networkClient;
    private SpriteFont _font;
    private Texture2D _pixel;

    // Chat Input State
    private bool _isTyping = false;
    private string _currentInput = "";
    private System.TimeSpan _cursorBlinkTimer;
    private bool _cursorVisible;
    private Microsoft.Xna.Framework.Input.KeyboardState _lastKeyboardState;

    public UiPartyWindow(PartyState partyState, Networking.NetworkClient networkClient) : base(new Rectangle(10, 200, 200, 150))
    {
        _partyState = partyState;
        _networkClient = networkClient;
        Visible = false; // Hidden by default if no party
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Fonts/DefaultFont");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime, Microsoft.Xna.Framework.Input.MouseState mouse, Microsoft.Xna.Framework.Input.KeyboardState keyboard)
    {
        base.Update(gameTime, mouse, keyboard);

        if (_partyState.PartyId == null) return;

        // Toggle typing with Enter
        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) && _lastKeyboardState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            if (_isTyping)
            {
                // Send message
                if (!string.IsNullOrWhiteSpace(_currentInput))
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new TWL.Shared.Domain.DTO.PartyChatRequest { Content = _currentInput });
                    _networkClient.SendNetMessage(new TWL.Shared.Net.Network.NetMessage
                    {
                        Op = TWL.Shared.Net.Network.Opcode.PartyChatRequest,
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
            // Simple text input (letters, numbers, space, backspace)
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
                            _currentInput += keyString; // Simplified input handling
                        }
                    }
                }
            }
        }

        _lastKeyboardState = keyboard;

        // Cursor Blink
        if (_isTyping)
        {
            _cursorBlinkTimer += gameTime.ElapsedGameTime;
            if (_cursorBlinkTimer.TotalMilliseconds > 500)
            {
                _cursorVisible = !_cursorVisible;
                _cursorBlinkTimer = System.TimeSpan.Zero;
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        // Only show if we are in a party
        if (_partyState.PartyId == null)
        {
            Visible = false;
            return;
        }

        Visible = true;

        // Background
        sb.Draw(_pixel, _bounds, new Color(0, 0, 0, 128));

        // Header
        sb.DrawString(_font, "Party", new Vector2(_bounds.X + 5, _bounds.Y + 5), Color.Yellow);

        int yOffset = 25;
        foreach (var member in _partyState.Members)
        {
            var isLeader = member.CharacterId == _partyState.LeaderId;
            var nameColor = isLeader ? Color.Gold : Color.White;

            // Name & Level
            sb.DrawString(_font, $"{member.Name} (Lv.{member.Level})", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), nameColor);

            // HP Bar
            yOffset += 20;
            float hpPct = (member.MaxHp > 0) ? (float)member.CurrentHp / member.MaxHp : 0;
            sb.Draw(_pixel, new Rectangle(_bounds.X + 5, _bounds.Y + yOffset, (int)(100 * hpPct), 5), Color.Red);
            // Background HP
            sb.Draw(_pixel, new Rectangle(_bounds.X + 5, _bounds.Y + yOffset, 100, 5), new Color(50, 0, 0, 128));

            // MP Bar
            yOffset += 7;
            float mpPct = (member.MaxMp > 0) ? (float)member.CurrentMp / member.MaxMp : 0;
            sb.Draw(_pixel, new Rectangle(_bounds.X + 5, _bounds.Y + yOffset, (int)(100 * mpPct), 5), Color.Blue);
            // Background MP
            sb.Draw(_pixel, new Rectangle(_bounds.X + 5, _bounds.Y + yOffset, 100, 5), new Color(0, 0, 50, 128));

            yOffset += 15;
        }

        // Chat Overlay (Simple)
        yOffset += 10;

        // TakeLast is LINQ, ensure using System.Linq
        var recentChat = _partyState.ChatLog.Skip(System.Math.Max(0, _partyState.ChatLog.Count - 3)).Take(3);

        foreach (var msg in recentChat)
        {
             sb.DrawString(_font, $"{msg.SenderName}: {msg.Content}", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.Cyan);
             yOffset += 15;
        }

        // Input Field
        yOffset += 5;
        if (_isTyping)
        {
            var inputText = _currentInput + (_cursorVisible ? "|" : "");
            sb.DrawString(_font, $"> {inputText}", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.White);
        }
        else
        {
            sb.DrawString(_font, "[Enter] to Chat", new Vector2(_bounds.X + 5, _bounds.Y + yOffset), Color.Gray);
        }
    }
}
