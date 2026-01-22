using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Battle;

public class StatusEffectInstance
{
    public SkillEffectTag Tag { get; set; }
    public float Value { get; set; }
    public int TurnsRemaining { get; set; }
    public string? Param { get; set; }

    public StatusEffectInstance(SkillEffectTag tag, float value, int duration, string? param = null)
    {
        Tag = tag;
        Value = value;
        TurnsRemaining = duration;
        Param = param;
    }
}
