using System;

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
}
