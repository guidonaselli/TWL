using System.Collections.Generic;

namespace TWL.Shared.Domain.Graphics;

public class CharacterAppearance
{
    public string BaseBodyId { get; set; } = "default_body";
    public PaletteType Palette { get; set; } = PaletteType.Normal;
    public List<AvatarPart> EquipmentVisuals { get; set; } = new();

    public CharacterAppearance() { }
}
