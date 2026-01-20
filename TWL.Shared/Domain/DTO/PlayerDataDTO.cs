using TWL.Shared.Domain.Graphics;

namespace TWL.Shared.Domain.DTO;

public class PlayerDataDTO
{
    public int PlayerId { get; set; }
    public string UserName { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }

    public int MaxHp { get; set; }

    public CharacterAppearance Appearance { get; set; } = new();
    // ... etc.
}