// UiGameplay.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using TWL.Client.UI;
using TWL.Client.Presentation.Services;
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
            _invWindow.Visible = false;
            _invWindow.Active = false;
            AddWindow(_invWindow);
        }

        public bool IsInventoryVisible => _invWindow.Visible;

        public void LoadContent(ContentManager content, GraphicsDevice gd)
        {
            _font      = content.Load<SpriteFont>("Fonts/DefaultFont");

            // Create placeholder textures if files are missing
            try { _panelBg = content.Load<Texture2D>("UI/hud_panel_bg"); }
            catch
            {
                 _panelBg = new Texture2D(gd, 1, 1);
                 _panelBg.SetData(new[] { new Color(0, 0, 0, 128) });
            }

            try { _miniMapBg = content.Load<Texture2D>("UI/minimap_bg"); }
            catch
            {
                 _miniMapBg = new Texture2D(gd, 1, 1);
                 _miniMapBg.SetData(new[] { new Color(0, 0, 0, 128) });
            }

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
            // *** Barra superior izquierda: HP / SP / XP ***
            // Fondo del panel
            sb.Draw(_panelBg, new Rectangle(10, 10, 320, 150), Color.White);

            // Icono del personaje (placeholder)
            sb.DrawRectangle(new Rectangle(20, 20, 60, 60), Color.Gray, 2);
            sb.DrawString(_font, Loc.T("UI_Icon"), new Vector2(25, 40), Color.Gray);

            int statsX = 90;

            // Nivel
            sb.DrawString(_font, 
                Loc.TF("UI_LevelFormat", _player.Level),
                new Vector2(statsX, 20), Color.Yellow);

            // HP
            var hpPct = (float)_player.Health / _player.MaxHealth;
            sb.DrawString(_font,
                Loc.TF("UI_HpFormat", _player.Health, _player.MaxHealth),
                new Vector2(statsX, 40), Color.Red);
            sb.FillRectangle(
                new Rectangle(statsX, 60, (int)(200 * hpPct), 10),
                Color.Red);
            sb.DrawRectangle(
                new Rectangle(statsX, 60, 200, 10), Color.DarkRed, 1);

            // SP
            var spPct = (float)_player.Sp / _player.MaxSp;
            sb.DrawString(_font,
                Loc.TF("UI_SpFormat", _player.Sp, _player.MaxSp),
                new Vector2(statsX, 75), Color.CornflowerBlue);
            sb.FillRectangle(
                new Rectangle(statsX, 95, (int)(200 * spPct), 10),
                Color.CornflowerBlue);
            sb.DrawRectangle(
                new Rectangle(statsX, 95, 200, 10), Color.DarkBlue, 1);

            // XP
            var xpPct = (float)_player.Exp / _player.ExpToNextLevel;
            sb.DrawString(_font,
                Loc.TF("UI_XpFormat", _player.Exp, _player.ExpToNextLevel),
                new Vector2(statsX, 110), Color.LightGreen);
            sb.FillRectangle(
                new Rectangle(statsX, 130, (int)(200 * xpPct), 6),
                Color.LightGreen);
            sb.DrawRectangle(
                new Rectangle(statsX, 130, 200, 6), Color.DarkGreen, 1);

            // *** Minimap (placeholder) ***
            var mmPos = new Vector2(
                sb.GraphicsDevice.Viewport.Width - 160, 10);
            sb.Draw(_miniMapBg,
                    new Rectangle((int)mmPos.X, (int)mmPos.Y, 150, 150),
                    Color.White);
            sb.DrawString(_font, Loc.T("UI_Minimap"), mmPos + new Vector2(10,10),
                          Color.White);

            // *** Inventario y ventanas ***
            base.Draw(sb);
        }
    }
}
