namespace TWL.Shared.Domain.Skills;

public enum StatType
{
    Str,
    Con,
    Int,
    Wis,
    Agi,
    Atk,
    Def,
    Mat,
    Mdf,
    Spd
}

public class SkillScaling
{
    public StatType Stat { get; set; }
    public float Coefficient { get; set; }
}