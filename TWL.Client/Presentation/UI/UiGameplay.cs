﻿// UiGameplay.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using TWL.Client.UI;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.UI
{
    /// <summary>
    /// Administra el HUD en pantalla: 
    /// • Barra de vida y XP en la esquina superior izquierda  
    /// • Minimap en la superior derecha (placeholder)  
    /// • Inventario (toggle con I)  
    /// • Mensajes de sistema en parte inferior  
    /// </summary>
    public class UiGameplay : UiManager
    {
        private readonly UiInventoryWindow _invWindow;
        private readonly PlayerCharacter   _player;

        // recursos HUD
        private SpriteFont _font      = null!;
        private Texture2D  _panelBg   = null!;   // panel barra HP/XP
        private Texture2D  _miniMapBg = null!;   // placeholder minimap

        public UiGameplay(PlayerCharacter player)
        {
            _player     = player;
            _invWindow = new UiInventoryWindow(
                new Rectangle(40,40,320,480),
                _player.Inventory);
            AddWindow(_invWindow);
        }

        public bool IsInventoryVisible => _invWindow.Visible;

        public void LoadContent(ContentManager content)
        {
            _font      = content.Load<SpriteFont>("Fonts/DefaultFont");
            _panelBg   = content.Load<Texture2D>("UI/hud_panel_bg");
            _miniMapBg = content.Load<Texture2D>("UI/minimap_bg");
            _invWindow.LoadContent(content);
        }

        public void ToggleInventory()
        {
            var v = !_invWindow.Visible;
            _invWindow.Visible = v;
            _invWindow.Active  = v;
        }

        public void Update(GameTime time,
                           MouseState ms,
                           KeyboardState ks)
        {
            // si el inventario está abierto, deja que UiManager lo gestione
            base.Update(time, ms, ks);
        }

        public void Draw(SpriteBatch sb)
        {
            // *** Barra superior izquierda: HP / XP ***
            sb.Draw(_panelBg, new Vector2(10,10), Color.White);
            // nivel
            sb.DrawString(_font, 
                $"Nivel: {_player.Level}", 
                new Vector2(20,20), Color.Yellow);
            // HP
            var hpPct = (float)_player.Health / _player.MaxHealth;
            sb.DrawString(_font,
                $"HP: {_player.Health}/{_player.MaxHealth}",
                new Vector2(20,40), Color.Red);
            sb.DrawRectangle(
                new Rectangle(20,60, (int)(200*hpPct), 10),
                Color.Red);

            // XP
            var xpPct = (float)_player.Exp / _player.ExpToNextLevel;
            sb.DrawString(_font,
                $"XP: {_player.Exp}/{_player.ExpToNextLevel}",
                new Vector2(20,80), Color.LightGreen);
            sb.DrawRectangle(
                new Rectangle(20,100, (int)(200*xpPct), 6),
                Color.LightGreen);

            // *** Minimap (placeholder) ***
            var mmPos = new Vector2(
                sb.GraphicsDevice.Viewport.Width - 160, 10);
            sb.Draw(_miniMapBg,
                    new Rectangle((int)mmPos.X, (int)mmPos.Y, 150, 150),
                    Color.White);
            sb.DrawString(_font, "Minimap", mmPos + new Vector2(10,10),
                          Color.White);

            // *** Inventario y ventanas ***
            base.Draw(sb);
        }
    }
}
