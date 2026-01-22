namespace TWL.Shared.Domain.Skills;

public class SkillEffect
{
    public SkillEffectTag Tag { get; set; }
    public float Value { get; set; } // Base power or multiplier
    public int Duration { get; set; } // Turns
    public float Chance { get; set; } = 1.0f;
    public string? Param { get; set; } // Extra param (e.g., stat name for buff)
}
