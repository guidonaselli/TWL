using System.Collections.Generic;
using TWL.Shared.Domain.Battle;

namespace TWL.Shared.Domain.Requests;

public class CombatResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public int Damage { get; set; }
    public int NewTargetHp { get; set; }
    public bool TargetDied { get; set; }
    public List<StatusEffectInstance> AddedEffects { get; set; } = new();
}
