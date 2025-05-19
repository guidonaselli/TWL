namespace TWL.Shared.Domain.DTO;

public class PlayerDataDTO
{
    public int PlayerId { get; set; }
    public string UserName { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }

    public int MaxHp { get; set; }
    // ... etc.
}