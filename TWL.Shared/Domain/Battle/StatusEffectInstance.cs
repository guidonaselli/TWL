using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Battle;

public class StatusEffectInstance
{
    public SkillEffectTag Tag { get; set; }
    public float Value { get; set; }
    public int TurnsRemaining { get; set; }
    public string? Param { get; set; }

    // Metadata for Stacking/Logic
    public int SourceSkillId { get; set; }
    public int StackCount { get; set; } = 1;
    public int MaxStacks { get; set; } = 1;
    public StackingPolicy StackingPolicy { get; set; }
    public string? ConflictGroup { get; set; }
    public int Priority { get; set; }

    public StatusEffectInstance(SkillEffectTag tag, float value, int duration, string? param = null)
    {
        Tag = tag;
        Value = value;
        TurnsRemaining = duration;
        Param = param;
    }
}
