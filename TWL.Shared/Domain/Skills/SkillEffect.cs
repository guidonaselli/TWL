namespace TWL.Shared.Domain.Skills;

public class SkillEffect
{
    public SkillEffectTag Tag { get; set; }
    public float Value { get; set; } // Base power or multiplier
    public int Duration { get; set; } // Turns
    public float Chance { get; set; } = 1.0f;
    public string? Param { get; set; } // Extra param (e.g., stat name for buff)

    // Stacking & Conflict Logic
    public StackingPolicy StackingPolicy { get; set; } = StackingPolicy.NoStackOverwrite;
    public int MaxStacks { get; set; } = 1; // Used if StackingPolicy is StackUpToN
    public int Priority { get; set; } = 0; // Higher priority overrides/prevents lower priority in same group
    public string? ConflictGroup { get; set; } // e.g. "HardControl", "AttackBuff"

    // Resistance & Outcome
    public OutcomeModel Outcome { get; set; } = OutcomeModel.Full;
    public List<string> ResistanceTags { get; set; } = new();
}