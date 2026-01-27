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

        // Dictionary to store equipment textures by key (Slot_AssetId_Dir)
        private System.Collections.Generic.Dictionary<string, Texture2D> _equipmentTextures = new();

        // Cache for current frame textures to avoid dictionary lookups/allocations in Draw
        private System.Collections.Generic.List<Texture2D> _currentFrameOverlays = new();
        private FacingDirection _lastDirection = FacingDirection.Down; // Force update on first frame

        public PlayerView(PlayerCharacter player)
            => _player = player;

        public void Load(ContentManager content, GraphicsDevice gd)
        {
            // Dispose logic is now handled by the Scene or Cache clearing,
            // but we reset local references.
            _equipmentTextures.Clear();

            // Note: Hardcoding to RegularMale/Base/Idle for vertical slice as per instructions (Finish the game not the engine)
            var basePath = "Sprites/Characters/RegularMale/Base/Idle";
            var clientColors = new PlayerColors
            {
                Skin = ColorHelper.FromHex(_player.Colors.SkinColor),
                Hair = ColorHelper.FromHex(_player.Colors.HairColor),
                Eye = ColorHelper.FromHex(_player.Colors.EyeColor),
                Cloth = ColorHelper.FromHex(_player.Colors.ClothColor ?? "#888888")
            };

            _down  = GetSwappedTexture(content, gd, $"{basePath}/player_down", clientColors);
            _up    = GetSwappedTexture(content, gd, $"{basePath}/player_up", clientColors);
            _left  = GetSwappedTexture(content, gd, $"{basePath}/player_left", clientColors);
            _right = GetSwappedTexture(content, gd, $"{basePath}/player_right", clientColors);

            // Load Equipment Visuals
            foreach (var part in _player.Appearance.EquipmentVisuals)
            {
                if (string.IsNullOrEmpty(part.AssetId)) continue;

                // Load 4 directions for each part
                LoadPartTexture(content, gd, part, "down");
                LoadPartTexture(content, gd, part, "up");
                LoadPartTexture(content, gd, part, "left");
                LoadPartTexture(content, gd, part, "right");
            }
        }

        private Texture2D GetSwappedTexture(ContentManager content, GraphicsDevice gd, string path, PlayerColors colors)
        {
            string colorKey = $"{colors.Skin.PackedValue}_{colors.Hair.PackedValue}_{colors.Eye.PackedValue}_{colors.Cloth.PackedValue}"; // loc: ignore
            string cacheKey = $"{path}_{colorKey}"; // loc: ignore

            var cached = PaletteTextureCache.Get(cacheKey);
            if (cached != null) return cached;

            var original = content.Load<Texture2D>(path);
            var swapped = PaletteSwapper.Swap(original, colors, gd);

            PaletteTextureCache.Add(cacheKey, swapped);
            return swapped;
        }

        private void LoadPartTexture(ContentManager content, GraphicsDevice gd, TWL.Shared.Domain.Graphics.AvatarPart part, string dir)
        {
             try
             {
                 // Path convention: Sprites/Items/{AssetId}/{dir}
                 string path = $"Sprites/Items/{part.AssetId}/{dir}";
                 var tex = content.Load<Texture2D>(path);

                 // TODO: If we want to palette swap items (e.g. dyed armor), we would use GetSwappedTexture here too.
                 // For now, we assume items are pre-colored or fixed.

                 string key = $"{part.AssetId}_{dir}"; // loc: ignore
                 _equipmentTextures[key] = tex;
             }
             catch
             {
                 // Ignore missing assets for now
             }
        }

        public void Update(GameTime gameTime)
        {
            // Optimize: Update overlay cache only when direction changes
            if (_player.CurrentDirection != _lastDirection)
            {
                _lastDirection = _player.CurrentDirection;
                UpdateOverlayCache();
            }
        }

        private void UpdateOverlayCache()
        {
            _currentFrameOverlays.Clear();
            string dirStr = _player.CurrentDirection switch
            {
                FacingDirection.Up => "up",
                FacingDirection.Left => "left",
                FacingDirection.Right => "right",
                _ => "down"
            };

            foreach (var part in _player.Appearance.EquipmentVisuals)
            {
                string key = $"{part.AssetId}_{dirStr}"; // loc: ignore
                if (_equipmentTextures.TryGetValue(key, out var tex))
                {
                    _currentFrameOverlays.Add(tex);
                }
            }
        }

        public void Dispose()
        {
            // Do NOT dispose generated textures here if they are cached globally.
            // Only clear local references.
            _down = _up = _left = _right = null;
            _equipmentTextures.Clear();
            _currentFrameOverlays.Clear();
        }

        public void Draw(SpriteBatch sb)
        {
            // 1. Draw Base Body
            var tex = _down;

            switch (_player.CurrentDirection)
            {
                case FacingDirection.Up:    tex = _up;    break;
                case FacingDirection.Left:  tex = _left;  break;
                case FacingDirection.Right: tex = _right; break;
            }
            if (tex != null)
                sb.Draw(tex, _player.Position, Color.White);

            // 2. Draw Equipment Overlay
            // Draw cached overlays
            foreach (var overlay in _currentFrameOverlays)
            {
                sb.Draw(overlay, _player.Position, Color.White);
            }
        }
    }
}
