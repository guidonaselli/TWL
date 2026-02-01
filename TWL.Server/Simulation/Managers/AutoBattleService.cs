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

    // Configurable thresholds
    public int MinSpThreshold { get; set; } = 10;
    public float CriticalHpPercent { get; set; } = 0.4f;

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
                .Where(a => a.Hp > 0 && (float)a.Hp / a.MaxHealth < CriticalHpPercent)
                .OrderBy(a => a.Hp)
                .FirstOrDefault();

            if (lowHpAlly != null)
            {
                var healSkillId = FindBestSkill(actor, SkillEffectTag.Heal, SkillTargetType.SingleAlly, ignoreThreshold: true);
                if (healSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, lowHpAlly.Id, healSkillId.Value);
                }
            }
        }

        // 2. Check Debuffs -> Cleanse
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced)
        {
            // Check for Seal or Stat Debuffs
            var debuffedAlly = allies.FirstOrDefault(a => a.Hp > 0 && a.StatusEffects.Any(e => e.Tag == SkillEffectTag.Seal || e.Tag == SkillEffectTag.DebuffStats));
            if (debuffedAlly != null)
            {
                var cleanseSkillId = FindBestSkill(actor, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly, ignoreThreshold: true);
                if (cleanseSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, debuffedAlly.Id, cleanseSkillId.Value);
                }
            }
        }

        // 3. Check Enemy Buffs -> Dispel
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced || policy == AutoBattlePolicy.Aggressive)
        {
            var buffedEnemy = enemies.FirstOrDefault(e => e.Hp > 0 && e.StatusEffects.Any(s => s.Tag == SkillEffectTag.BuffStats || s.Tag == SkillEffectTag.Shield));
            if (buffedEnemy != null)
            {
                var dispelSkillId = FindBestSkill(actor, SkillEffectTag.Dispel, SkillTargetType.SingleEnemy, ignoreThreshold: false);
                if (dispelSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, buffedEnemy.Id, dispelSkillId.Value);
                }
            }
        }

        // 4. Control/Seal
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced || policy == AutoBattlePolicy.Aggressive)
        {
            // Find unsealed enemy
            var targetEnemy = enemies
                .Where(e => e.Hp > 0 && !e.StatusEffects.Any(s => s.Tag == SkillEffectTag.Seal))
                .OrderByDescending(e => e.Atk + e.Mat) // Target strongest
                .FirstOrDefault();

            if (targetEnemy != null)
            {
                var sealSkillId = FindBestSkill(actor, SkillEffectTag.Seal, SkillTargetType.SingleEnemy, ignoreThreshold: false);
                if (sealSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, targetEnemy.Id, sealSkillId.Value);
                }
            }
        }

        // 5. Apply Buffs
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced)
        {
            foreach (var skillId in actor.KnownSkills)
            {
                var skill = _skillCatalog.GetSkillById(skillId);
                if (skill == null || actor.IsSkillOnCooldown(skillId) || actor.Sp < skill.SpCost) continue;

                // Only consider SingleAlly buffs for now to keep it simple
                if (skill.TargetType != SkillTargetType.SingleAlly && skill.TargetType != SkillTargetType.AllAllies) continue;

                var buffEffect = skill.Effects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats);
                if (buffEffect != null)
                {
                    // Find ally without conflicting buff
                    var targetAlly = allies.FirstOrDefault(a => a.Hp > 0 && !HasConflictingBuff(a, buffEffect));

                    if (targetAlly != null)
                    {
                        // Basic SP check for non-critical actions
                        if ((actor.Sp - skill.SpCost) >= MinSpThreshold)
                        {
                            return CombatAction.UseSkill(actor.Id, targetAlly.Id, skillId);
                        }
                    }
                }
            }
        }

        // 6. Attack
        var target = GetBestTarget(enemies);
        if (target != null)
        {
            // Only use offensive skills if SP is above threshold
            if (actor.Sp > MinSpThreshold)
            {
                var damageSkillId = FindBestSkill(actor, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, ignoreThreshold: false);
                if (damageSkillId.HasValue)
                {
                    return CombatAction.UseSkill(actor.Id, target.Id, damageSkillId.Value);
                }
            }
            // Fallback to basic attack
            return CombatAction.Attack(actor.Id, target.Id);
        }

        // Fallback: Defend
        return CombatAction.Defend(actor.Id);
    }

    private bool HasConflictingBuff(ServerCharacter character, SkillEffect newEffect)
    {
        if (!string.IsNullOrEmpty(newEffect.ConflictGroup))
        {
            return character.StatusEffects.Any(e => e.ConflictGroup == newEffect.ConflictGroup);
        }
        // Fallback to Param check for basic buffs
        if (newEffect.Tag == SkillEffectTag.BuffStats && !string.IsNullOrEmpty(newEffect.Param))
        {
            return character.StatusEffects.Any(e => e.Tag == SkillEffectTag.BuffStats && e.Param == newEffect.Param);
        }
        return false;
    }

    private ServerCharacter? GetBestTarget(List<ServerCharacter> enemies)
    {
        // Simple heuristic: Lowest HP % to secure kill
        return enemies
            .Where(e => e.Hp > 0)
            .OrderBy(e => (float)e.Hp / e.MaxHealth)
            .FirstOrDefault();
    }

    private int? FindBestSkill(ServerCharacter actor, SkillEffectTag effectTag, SkillTargetType targetType, bool ignoreThreshold)
    {
        // Heuristic: Pick highest SP Cost as proxy for "Best".
        int? bestSkillId = null;
        int maxCost = -1;

        foreach (var skillId in actor.KnownSkills)
        {
            var skill = _skillCatalog.GetSkillById(skillId);
            if (skill == null) continue;

            if (actor.IsSkillOnCooldown(skill.SkillId)) continue;

            if (skill.SpCost > actor.Sp) continue;

            if (!ignoreThreshold && (actor.Sp - skill.SpCost) < MinSpThreshold) continue;

            // Check target type compatibility (loose check)
            bool targetMatch = false;

            if (targetType == SkillTargetType.SingleAlly && (skill.TargetType == SkillTargetType.SingleAlly || skill.TargetType == SkillTargetType.AllAllies)) targetMatch = true;
            if (targetType == SkillTargetType.SingleEnemy && (skill.TargetType == SkillTargetType.SingleEnemy || skill.TargetType == SkillTargetType.AllEnemies || skill.TargetType == SkillTargetType.RowEnemies)) targetMatch = true;

            if (targetMatch)
            {
                if (skill.Effects.Any(e => e.Tag == effectTag))
                {
                    if (skill.SpCost > maxCost)
                    {
                        maxCost = skill.SpCost;
                        bestSkillId = skillId;
                    }
                }
            }
        }
        return bestSkillId;
    }
}
