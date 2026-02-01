namespace TWL.Shared.Domain.Skills;

public class SkillHitRules
{
    public float BaseChance { get; set; } = 1.0f;
    public string? StatDependence { get; set; } // e.g., "Int-Wis"
    public float MinChance { get; set; } = 0.0f;
    public float MaxChance { get; set; } = 1.0f;
}