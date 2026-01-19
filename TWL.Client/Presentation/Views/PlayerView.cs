// TWL.Client/Presentation/Views/PlayerView.cs
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TWL.Shared.Domain.Characters;
using TWL.Client.Presentation.Graphics;

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

            var d = content.Load<Texture2D>("Sprites/.../player_down");
            var u = content.Load<Texture2D>("Sprites/.../player_up");
            var l = content.Load<Texture2D>("Sprites/.../player_left");
            var r = content.Load<Texture2D>("Sprites/.../player_right");

            _down  = PaletteSwapper.Swap(d, _player.Colors, gd);
            _up    = PaletteSwapper.Swap(u, _player.Colors, gd);
            _left  = PaletteSwapper.Swap(l, _player.Colors, gd);
            _right = PaletteSwapper.Swap(r, _player.Colors, gd);
        }

        public void Dispose()
        {
            _down?.Dispose();
            _up?.Dispose();
            _left?.Dispose();
            _right?.Dispose();
            _down = _up = _left = _right = null;
        }

        public void Update(GameTime gt) { }

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