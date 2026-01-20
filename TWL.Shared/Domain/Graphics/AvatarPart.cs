using TWL.Shared.Domain.Requests;

namespace TWL.Shared.Domain.Graphics;

public class AvatarPart
{
    public EquipmentSlot Slot { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public PaletteType? PaletteOverride { get; set; }

    public AvatarPart() { }

    public AvatarPart(EquipmentSlot slot, string assetId, PaletteType? paletteOverride = null)
    {
        Slot = slot;
        AssetId = assetId;
        PaletteOverride = paletteOverride;
    }
}
