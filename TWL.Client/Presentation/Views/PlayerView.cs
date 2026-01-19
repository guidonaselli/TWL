// TWL.Client/Presentation/Views/PlayerView.cs
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TWL.Shared.Domain.Characters;
using TWL.Client.Presentation.Graphics;
using TWL.Client.Presentation.Models;
using TWL.Client.Presentation.Helpers;

namespace TWL.Client.Presentation.Views
{
    public class PlayerView : IDisposable
    {
        private readonly PlayerCharacter _player;
        private Texture2D _down, _up, _left, _right;

        public PlayerView(PlayerCharacter player)
            => _player = player;

        public void Load(ContentManager content, GraphicsDevice gd)
        {
            // Ensure we clean up any previous textures if Load is called multiple times
            Dispose();

            // Note: Hardcoding to RegularMale/Base/Idle for vertical slice as per instructions (Finish the game not the engine)
            // Ideally this would come from PlayerCharacter.BodyType or similar.
            var d = content.Load<Texture2D>("Sprites/Characters/RegularMale/Base/Idle/player_down");
            var u = content.Load<Texture2D>("Sprites/Characters/RegularMale/Base/Idle/player_up");
            var l = content.Load<Texture2D>("Sprites/Characters/RegularMale/Base/Idle/player_left");
            var r = content.Load<Texture2D>("Sprites/Characters/RegularMale/Base/Idle/player_right");

            var clientColors = new PlayerColors
            {
                Skin = ColorHelper.FromHex(_player.Colors.SkinColor),
                Hair = ColorHelper.FromHex(_player.Colors.HairColor),
                Eye = ColorHelper.FromHex(_player.Colors.EyeColor)
            };

            _down  = PaletteSwapper.Swap(d, clientColors, gd);
            _up    = PaletteSwapper.Swap(u, clientColors, gd);
            _left  = PaletteSwapper.Swap(l, clientColors, gd);
            _right = PaletteSwapper.Swap(r, clientColors, gd);
        }

        public void Update(GameTime gameTime)
        {
            // Animation logic can go here
        }

        public void Dispose()
        {
            _down?.Dispose();
            _up?.Dispose();
            _left?.Dispose();
            _right?.Dispose();
            _down = _up = _left = _right = null;
        }

        public void Draw(SpriteBatch sb)
        {
            var tex = _down;
            switch (_player.CurrentDirection)
            {
                case FacingDirection.Up:    tex = _up;    break;
                case FacingDirection.Left:  tex = _left;  break;
                case FacingDirection.Right: tex = _right; break;
            }
            if (tex != null)
                sb.Draw(tex, _player.Position, Color.White);
        }
    }
}
