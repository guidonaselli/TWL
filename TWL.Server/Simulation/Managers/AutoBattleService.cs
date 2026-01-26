using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Server.Simulation.Managers;

public enum AutoBattlePolicy
{
    Balanced,
    Aggressive,
    Defensive,
    Supportive
}

public class AutoBattleService
{
    private readonly ISkillCatalog _skillCatalog;

    public AutoBattleService(ISkillCatalog skillCatalog)
    {
        _skillCatalog = skillCatalog;
    }

    public CombatAction SelectAction(
        ServerCharacter actor,
        List<ServerCharacter> allies,
        List<ServerCharacter> enemies,
        int seed,
        AutoBattlePolicy policy = AutoBattlePolicy.Balanced)
    {
        var rng = new Random(seed);

        // 1. Check Critical Health (Self or Ally) -> Heal
        if (policy != AutoBattlePolicy.Aggressive)
        {
            var lowHpAlly = allies
                .Where(a => a.Hp > 0 && (float)a.Hp / a.MaxHealth < 0.4f)
                .OrderBy(a => a.Hp)
                .FirstOrDefault();

            if (lowHpAlly != null)
            {
                // Find Heal Skill
                var healSkillId = FindBestSkill(actor, SkillEffectTag.Heal, SkillTargetType.SingleAlly);
                if (healSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, lowHpAlly.Id, healSkillId.Value);
                }
            }
        }

        // 2. Check Debuffs -> Cleanse
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced)
        {
            var debuffedAlly = allies.FirstOrDefault(a => a.StatusEffects.Any(e => e.Tag == SkillEffectTag.Seal || e.Tag == SkillEffectTag.DebuffStats));
            if (debuffedAlly != null)
            {
                var cleanseSkillId = FindBestSkill(actor, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly);
                if (cleanseSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, debuffedAlly.Id, cleanseSkillId.Value);
                }
            }
        }

        // 3. Attack
        // Find damage skill
        var damageSkillId = FindBestSkill(actor, SkillEffectTag.Damage, SkillTargetType.SingleEnemy);

        // Target selection: Lowest HP or Element Advantage?
        // Simple heuristic: Lowest HP % to secure kill
        var target = enemies
            .Where(e => e.Hp > 0)
            .OrderBy(e => (float)e.Hp / e.MaxHealth)
            .FirstOrDefault();

        if (target != null)
        {
            if (damageSkillId.HasValue && actor.Sp >= _skillCatalog.GetSkillById(damageSkillId.Value)?.SpCost)
            {
                return CombatAction.UseSkill(actor.Id, target.Id, damageSkillId.Value);
            }

            // Default Attack
             return CombatAction.Attack(actor.Id, target.Id);
        }

        // Fallback: Defend
        return CombatAction.Defend(actor.Id);
    }

    private int? FindBestSkill(ServerCharacter actor, SkillEffectTag effectTag, SkillTargetType targetType)
    {
        // Simple search: Find known skill with effect tag and target type (approx)
        // In real impl, check cooldowns too.
        foreach (var skillId in actor.KnownSkills)
        {
            var skill = _skillCatalog.GetSkillById(skillId);
            if (skill == null) continue;
            if (skill.SpCost > actor.Sp) continue;

            // Check target type compatibility (loose check)
            bool targetMatch = false;

            // Allow SingleAlly skill for SingleAlly request
            if (targetType == SkillTargetType.SingleAlly && (skill.TargetType == SkillTargetType.SingleAlly || skill.TargetType == SkillTargetType.AllAllies)) targetMatch = true;

            // Allow SingleEnemy skill for SingleEnemy request
            if (targetType == SkillTargetType.SingleEnemy && (skill.TargetType == SkillTargetType.SingleEnemy || skill.TargetType == SkillTargetType.AllEnemies)) targetMatch = true;

            // Special case: Self target skills for defensive/buffs?

            if (targetMatch)
            {
                if (skill.Effects.Any(e => e.Tag == effectTag))
                {
                    return skillId;
                }
            }
        }
        return null;
    }
}
