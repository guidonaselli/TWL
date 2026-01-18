using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TWL.Shared.Domain.Characters;
using TWL.Client.Presentation.Graphics;
using TWL.Client.Presentation.Models;
using TWL.Client.Presentation.Helpers;

namespace TWL.Client.Presentation.Views
{
    public class PlayerView
    {
        private readonly PlayerCharacter _player;
        private Texture2D _down, _up, _left, _right;

        public PlayerView(PlayerCharacter player)
            => _player = player;

        public void Load(ContentManager content, GraphicsDevice gd)
        {
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

        public void Draw(SpriteBatch sb)
        {
            var tex = _down;
            switch (_player.CurrentDirection)
            {
                case FacingDirection.Up:    tex = _up;    break;
                case FacingDirection.Left:  tex = _left;  break;
                case FacingDirection.Right: tex = _right; break;
            }
            sb.Draw(tex, _player.Position, Color.White);
        }
    }
}
