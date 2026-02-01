// TWL.Client/Presentation/Views/PlayerView.cs

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TWL.Client.Presentation.Graphics;
using TWL.Client.Presentation.Helpers;
using TWL.Client.Presentation.Models;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Graphics;

namespace TWL.Client.Presentation.Views;

public class PlayerView : IDisposable
{
    // Cache for current frame textures to avoid dictionary lookups/allocations in Draw
    private readonly List<Texture2D> _currentFrameOverlays = new();

    // Dictionary to store equipment textures by key (Slot_AssetId_Dir)
    private readonly Dictionary<string, Texture2D> _equipmentTextures = new();
    private readonly PlayerCharacter _player;
    private Texture2D _bodyBaseDown;
    private Texture2D _bodyMapDown;
    private Texture2D _bodyBaseUp;
    private Texture2D _bodyMapUp;
    private Texture2D _bodyBaseSide;
    private Texture2D _bodyMapSide;
    private Texture2D _hairBaseDown;
    private Texture2D _hairMapDown;
    private Texture2D _hairBaseUp;
    private Texture2D _hairMapUp;
    private Texture2D _hairBaseSide;
    private Texture2D _hairMapSide;
    private Texture2D _down, _up, _left, _right;
    private FacingDirection _lastDirection = FacingDirection.Down; // Force update on first frame
    private Effect _paletteEffect;

    public PlayerView(PlayerCharacter player)
    {
        _player = player;
    }

    public void Dispose()
    {
        // Do NOT dispose generated textures here if they are cached globally.
        // Only clear local references.
        _down = _up = _left = _right = null;
        _bodyBaseDown = _bodyMapDown = _bodyBaseUp = _bodyMapUp = _bodyBaseSide = _bodyMapSide = null;
        _hairBaseDown = _hairMapDown = _hairBaseUp = _hairMapUp = _hairBaseSide = _hairMapSide = null;
        _paletteEffect = null;
        _equipmentTextures.Clear();
        _currentFrameOverlays.Clear();
    }

    public Effect PaletteEffect => _paletteEffect;

    public bool HasLayeredSprites =>
        _bodyBaseDown != null && _bodyMapDown != null
        && _bodyBaseUp != null && _bodyMapUp != null
        && _bodyBaseSide != null && _bodyMapSide != null
        && _hairBaseDown != null && _hairMapDown != null
        && _hairBaseUp != null && _hairMapUp != null
        && _hairBaseSide != null && _hairMapSide != null
        && _paletteEffect != null;

    public void Load(ContentManager content, GraphicsDevice gd)
    {
        // Dispose logic is now handled by the Scene or Cache clearing,
        // but we reset local references.
        _equipmentTextures.Clear();

        // Note: Hardcoding to RegularMale/Base/Idle for vertical slice as per instructions (Finish the game not the engine)
        var basePath = "Sprites/Characters/RegularMale/Base/Idle";
        _down = _up = _left = _right = null;

        try
        {
            _bodyBaseDown = content.Load<Texture2D>($"{basePath}/cuerpo_base");
            _bodyMapDown = content.Load<Texture2D>($"{basePath}/cuerpo_mapa");
            _hairBaseDown = content.Load<Texture2D>($"{basePath}/pelo_base");
            _hairMapDown = content.Load<Texture2D>($"{basePath}/pelo_mapa");

            _bodyBaseUp = content.Load<Texture2D>($"{basePath}/arriba_cuerpo_base");
            _bodyMapUp = content.Load<Texture2D>($"{basePath}/arriba_cuerpo_mapa");
            _hairBaseUp = content.Load<Texture2D>($"{basePath}/arriba_pelo_base");
            _hairMapUp = content.Load<Texture2D>($"{basePath}/arriba_pelo_mapa");

            _bodyBaseSide = content.Load<Texture2D>($"{basePath}/lateral_cuerpo_base");
            _bodyMapSide = content.Load<Texture2D>($"{basePath}/lateral_cuerpo_mapa");
            _hairBaseSide = content.Load<Texture2D>($"{basePath}/lateral_pelo_base");
            _hairMapSide = content.Load<Texture2D>($"{basePath}/lateral_pelo_mapa");

            _paletteEffect = content.Load<Effect>("Effects/PaletteSwap");
        }
        catch
        {
            _bodyBaseDown = _bodyMapDown = _bodyBaseUp = _bodyMapUp = _bodyBaseSide = _bodyMapSide = null;
            _hairBaseDown = _hairMapDown = _hairBaseUp = _hairMapUp = _hairBaseSide = _hairMapSide = null;
            _paletteEffect = null;
        }



        // Load Equipment Visuals
        foreach (var part in _player.Appearance.EquipmentVisuals)
        {
            if (string.IsNullOrEmpty(part.AssetId))
            {
                continue;
            }

            // Load 4 directions for each part
            LoadPartTexture(content, gd, part, "down");
            LoadPartTexture(content, gd, part, "up");
            LoadPartTexture(content, gd, part, "left");
            LoadPartTexture(content, gd, part, "right");
        }
    }

    private PlayerColors GetClientColors() =>
        new()
        {
            Skin = ColorHelper.FromHex(_player.Colors.SkinColor),
            Hair = ColorHelper.FromHex(_player.Colors.HairColor),
            Eye = ColorHelper.FromHex(_player.Colors.EyeColor),
            Cloth = ColorHelper.FromHex(_player.Colors.ClothColor ?? "#888888")
        };

    private Texture2D GetSwappedTexture(ContentManager content, GraphicsDevice gd, string path, PlayerColors colors)
    {
        var colorKey =
            $"{colors.Skin.PackedValue}_{colors.Hair.PackedValue}_{colors.Eye.PackedValue}_{colors.Cloth.PackedValue}"; // loc: ignore
        var cacheKey = $"{path}_{colorKey}"; // loc: ignore

        var cached = PaletteTextureCache.Get(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var original = content.Load<Texture2D>(path);
        var swapped = PaletteSwapper.Swap(original, colors, gd);

        PaletteTextureCache.Add(cacheKey, swapped);
        return swapped;
    }

    private void LoadPartTexture(ContentManager content, GraphicsDevice gd, AvatarPart part, string dir)
    {
        try
        {
            // Path convention: Sprites/Items/{AssetId}/{dir}
            var path = $"Sprites/Items/{part.AssetId}/{dir}";
            var tex = content.Load<Texture2D>(path);

            // TODO: If we want to palette swap items (e.g. dyed armor), we would use GetSwappedTexture here too.
            // For now, we assume items are pre-colored or fixed.

            var key = $"{part.AssetId}_{dir}"; // loc: ignore
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
        var dirStr = _player.CurrentDirection switch
        {
            FacingDirection.Up => "up",
            FacingDirection.Left => "left",
            FacingDirection.Right => "right",
            _ => "down"
        };

        foreach (var part in _player.Appearance.EquipmentVisuals)
        {
            var key = $"{part.AssetId}_{dirStr}"; // loc: ignore
            if (_equipmentTextures.TryGetValue(key, out var tex))
            {
                _currentFrameOverlays.Add(tex);
            }
        }
    }

    public void Draw(SpriteBatch sb)
    {
        DrawLegacyBase(sb);
        DrawEquipment(sb);
    }

    public void DrawLayeredBase(SpriteBatch sb)
    {
        if (!HasLayeredSprites)
        {
            return;
        }

        var colors = GetClientColors();

        var (bodyBase, bodyMap, hairBase, hairMap, effects) = GetLayeredSet();

        if (bodyBase == null || bodyMap == null || hairBase == null || hairMap == null)
        {
            return;
        }

        _paletteEffect.Parameters["MapTexture"]?.SetValue(bodyMap);
        _paletteEffect.Parameters["ColorPiel"]?.SetValue(colors.Skin.ToVector4());
        _paletteEffect.Parameters["ColorRopa"]?.SetValue(colors.Cloth.ToVector4());
        _paletteEffect.Parameters["ColorPelo"]?.SetValue(colors.Hair.ToVector4());

        sb.Draw(bodyBase, _player.Position, null, Color.White, 0f, Vector2.Zero, 1f, effects, 0f);

        _paletteEffect.Parameters["MapTexture"]?.SetValue(hairMap);
        _paletteEffect.Parameters["ColorPelo"]?.SetValue(colors.Hair.ToVector4());

        sb.Draw(hairBase, _player.Position, null, Color.White, 0f, Vector2.Zero, 1f, effects, 0f);
    }

    public void DrawEquipment(SpriteBatch sb)
    {
        foreach (var overlay in _currentFrameOverlays)
        {
            sb.Draw(overlay, _player.Position, Color.White);
        }
    }

    private void DrawLegacyBase(SpriteBatch sb)
    {
        if (HasLayeredSprites)
        {
            return;
        }

        var tex = _down;

        switch (_player.CurrentDirection)
        {
            case FacingDirection.Up: tex = _up; break;
            case FacingDirection.Left: tex = _left; break;
            case FacingDirection.Right: tex = _right; break;
        }

        if (tex != null)
        {
            sb.Draw(tex, _player.Position, Color.White);
        }
    }

    private (Texture2D BodyBase, Texture2D BodyMap, Texture2D HairBase, Texture2D HairMap, SpriteEffects Effects)
        GetLayeredSet()
    {
        switch (_player.CurrentDirection)
        {
            case FacingDirection.Up:
                return (_bodyBaseUp, _bodyMapUp, _hairBaseUp, _hairMapUp, SpriteEffects.None);
            case FacingDirection.Left:
                return (_bodyBaseSide, _bodyMapSide, _hairBaseSide, _hairMapSide, SpriteEffects.FlipHorizontally);
            case FacingDirection.Right:
                return (_bodyBaseSide, _bodyMapSide, _hairBaseSide, _hairMapSide, SpriteEffects.None);
            default:
                return (_bodyBaseDown, _bodyMapDown, _hairBaseDown, _hairMapDown, SpriteEffects.None);
        }
    }
}
