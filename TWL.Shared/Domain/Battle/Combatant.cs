using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Battle;

public class Combatant : ICombatant
{
    public Combatant(Character character)
    {
        Character = character;
        Atb = 0;
        IsDefending = false;
    }

    public int BattleId { get; set; } // Unique ID within the battle
    public Character Character { get; private set; }

    // ICombatant implementation
    public int Id => Character.Id;
    public string Name => Character.Name;
    public int Hp => Character.Health;
    public int MaxHp => Character.MaxHealth;
    public int Sp => Character.Sp;
    public int MaxSp => Character.MaxSp;

    public int Atk => Character.Atk;
    public int Def => Character.Def;
    public int Mat => Character.Mat;
    public int Mdf => Character.Mdf;
    public int Spd => Character.Spd;

    public Element CharacterElement => Character.CharacterElement;
    public Team Team => Character.Team;

    public double Atb { get; set; } // 0 to 100
    public bool IsDefending { get; set; }

    public int AttackBuffTurns { get; set; }

    public Dictionary<int, int> SkillRanks { get; set; } = new();

    public List<StatusEffectInstance> StatusEffects { get; } = new();

    IEnumerable<StatusEffectInstance> ICombatant.StatusEffects => StatusEffects;

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

    public void RemoveStatusEffect(SkillEffectTag tag) => StatusEffects.RemoveAll(e => e.Tag == tag);

    public void ResetTurn()
    {
        Atb = 0;
        IsDefending = false;
    }

    public bool IsReady() => Atb >= 100;

    public bool IsSkillOnCooldown(int skillId) => false;

    public IEnumerable<int> GetKnownSkillIds() => SkillRanks.Keys;

    public float GetResistance(string tag) => 0f;
}