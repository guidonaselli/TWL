using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class AutoBattleManager
{
    private readonly ISkillCatalog _skillCatalog;
    private readonly IRandomService _random;

    public AutoBattleManager(ISkillCatalog skillCatalog, IRandomService random)
    {
        _skillCatalog = skillCatalog;
        _random = random;
    }

    public UseSkillRequest? GetBestAction(ServerCombatant actor, IEnumerable<ServerCombatant> combatants)
    {
        var validTargets = combatants.Where(c => c.Hp > 0).ToList();
        var enemies = validTargets.Where(c => c.Team != actor.Team).ToList();
        var allies = validTargets.Where(c => c.Team == actor.Team).ToList();

        if (enemies.Count == 0) return null; // No enemies

        // 1. Get Available Skills
        var availableSkills = new List<Skill>();

        foreach (var kvp in actor.SkillMastery)
        {
            if (actor.IsSkillOnCooldown(kvp.Key)) continue;

            var skill = _skillCatalog.GetSkillById(kvp.Key);
            if (skill == null) continue;
            if (actor.Sp < skill.SpCost) continue;

            availableSkills.Add(skill);
        }

        if (availableSkills.Count == 0) return null;

        // 2. Heuristics

        // A. Survival / Sustain (Priority High)
        // If Self or Ally HP < 30% -> Heal
        var criticalAllies = allies.Where(a => (float)a.Hp / a.MaxHp < 0.3f).OrderBy(a => a.Hp).ToList();
        if (criticalAllies.Any())
        {
            var healSkill = availableSkills
                .Where(s => s.Effects.Any(e => e.Tag == SkillEffectTag.Heal))
                .OrderByDescending(s => s.Scaling.FirstOrDefault(sc => sc.Stat == StatType.Wis)?.Coefficient ?? 0)
                .FirstOrDefault();

            if (healSkill != null)
            {
                var target = criticalAllies.First();
                return new UseSkillRequest
                {
                    PlayerId = actor.Id,
                    SkillId = healSkill.SkillId,
                    TargetId = target.Id
                };
            }
        }

        // B. Support (Debuffed Ally -> Cleanse)
        foreach (var ally in allies)
        {
             if (ally.StatusEffects.Any(e => e.Tag == SkillEffectTag.DebuffStats || e.Tag == SkillEffectTag.Seal || e.Tag == SkillEffectTag.Burn))
             {
                 var cleanseSkill = availableSkills.FirstOrDefault(s => s.Effects.Any(e => e.Tag == SkillEffectTag.Cleanse));
                 if (cleanseSkill != null)
                 {
                     return new UseSkillRequest { PlayerId = actor.Id, SkillId = cleanseSkill.SkillId, TargetId = ally.Id };
                 }
             }
        }

        // C. Aggression (Killable Target / Max Damage)
        // Pick strongest attack (highest SP cost as proxy for power)
        var attackSkills = availableSkills
            .Where(s => s.Branch == SkillBranch.Physical || s.Branch == SkillBranch.Magical)
            .OrderByDescending(s => s.SpCost)
            .ToList();

        if (attackSkills.Any())
        {
            var bestAttack = attackSkills.First();

            // Target Selection:
            // If AoE, just pick random or first enemy as primary?
            // If Single, pick weakest enemy (lowest HP).
            var weakestEnemy = enemies.OrderBy(e => e.Hp).First();

            return new UseSkillRequest
            {
                PlayerId = actor.Id,
                SkillId = bestAttack.SkillId,
                TargetId = weakestEnemy.Id
            };
        }

        // Fallback: Just use random available skill on random valid target
        var randomSkill = availableSkills[_random.Next(0, availableSkills.Count)];

        ServerCombatant randomTarget;
        if (randomSkill.TargetType == SkillTargetType.SingleAlly || randomSkill.TargetType == SkillTargetType.AllAllies || randomSkill.TargetType == SkillTargetType.RowAllies)
        {
            randomTarget = allies[_random.Next(0, allies.Count)];
        }
        else if (randomSkill.TargetType == SkillTargetType.Self)
        {
            randomTarget = actor;
        }
        else
        {
            randomTarget = enemies[_random.Next(0, enemies.Count)];
        }

        return new UseSkillRequest
        {
            PlayerId = actor.Id,
            SkillId = randomSkill.SkillId,
            TargetId = randomTarget.Id
        };
    }
}
