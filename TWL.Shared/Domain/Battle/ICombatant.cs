using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Battle;

public interface ICombatant
{
    int Id { get; }
    string Name { get; }
    int Hp { get; }
    int MaxHp { get; }
    int Sp { get; }
    int MaxSp { get; }

    // Stats
    int Atk { get; }
    int Def { get; }
    int Mat { get; }
    int Mdf { get; }
    int Spd { get; }

    Element CharacterElement { get; }
    Team Team { get; }

    IEnumerable<StatusEffectInstance> StatusEffects { get; }

    bool IsSkillOnCooldown(int skillId);
    IEnumerable<int> GetKnownSkillIds();
    float GetResistance(string tag);
}
