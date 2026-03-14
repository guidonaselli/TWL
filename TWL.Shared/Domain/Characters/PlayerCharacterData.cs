using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Graphics;
using TWL.Shared.Net.Payloads;

namespace TWL.Shared.Domain.Characters;

public class PlayerCharacterData
{
    public int UserId { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int RebirthLevel { get; set; }
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
            UserId = -1,
            PlayerId = dto.PlayerId,
            Name = dto.UserName,
            Hp = dto.Hp,
            MaxHp = dto.MaxHp,
            Level = dto.Level,
            RebirthLevel = dto.RebirthLevel,
            PosX = dto.X,
            PosY = dto.Y,
            Appearance = dto.Appearance
        };
    }

    public static PlayerCharacterData FromLoginResponse(LoginResponseDto response, string name)
    {
        return new PlayerCharacterData
        {
            UserId = response.UserId,
            PlayerId = response.UserId,
            Name = name,
            Hp = response.Hp,
            MaxHp = response.MaxHp,
            PosX = response.PosX,
            PosY = response.PosY,
            RebirthLevel = response.RebirthLevel
        };
    }
}
