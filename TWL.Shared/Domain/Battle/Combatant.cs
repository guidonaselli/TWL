using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Battle;

public class Combatant
{
    public int BattleId { get; set; } // Unique ID within the battle
    public Character Character { get; private set; }
    public double Atb { get; set; } // 0 to 100
    public bool IsDefending { get; set; }

    public int AttackBuffTurns { get; set; }

    public List<StatusEffectInstance> StatusEffects { get; private set; } = new();

    public Combatant(Character character)
    {
        Character = character;
        Atb = 0;
        IsDefending = false;
    }

    public void AddStatusEffect(StatusEffectInstance effect)
    {
        // Simple stacking logic: Refresh duration if exists, else add
        var existing = StatusEffects.FirstOrDefault(e => e.Tag == effect.Tag);
        if (existing != null)
        {
            existing.TurnsRemaining = Math.Max(existing.TurnsRemaining, effect.TurnsRemaining);
            existing.Value = Math.Max(existing.Value, effect.Value); // Keep strongest
        }
        else
        {
            StatusEffects.Add(effect);
        }
    }

    public void RemoveStatusEffect(TWL.Shared.Domain.Skills.SkillEffectTag tag)
    {
        StatusEffects.RemoveAll(e => e.Tag == tag);
    }

    public void ResetTurn()
    {
        Atb = 0;
        IsDefending = false;
    }

    public bool IsReady()
    {
        return Atb >= 100;
    }
}
