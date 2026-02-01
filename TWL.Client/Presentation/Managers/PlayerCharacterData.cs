using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Graphics;

namespace TWL.Client.Presentation.Managers;

public class PlayerCharacterData
{
    public int UserId { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public CharacterAppearance Appearance { get; set; } = new();

    public static PlayerCharacterData FromDTO(PlayerDataDTO dto)
    {
        return new PlayerCharacterData
        {
            UserId = -1, // DTO might not have UserId easily accessible or it's different mapping
            PlayerId = dto.PlayerId,
            Name = dto.UserName,
            Hp = dto.Hp,
            MaxHp = dto.MaxHp,
            PosX = dto.X,
            PosY = dto.Y,
            Appearance = dto.Appearance
            // Level/Exp defaults or mapped if available
        };
    }
}