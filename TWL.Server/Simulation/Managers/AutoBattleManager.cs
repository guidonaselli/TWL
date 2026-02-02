using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class AutoBattleManager
{
    private readonly ISkillCatalog _skillCatalog;

    public int MinSpThreshold { get; set; } = 10;
    public float CriticalHpPercent { get; set; } = 0.3f;

    public AutoBattleManager(ISkillCatalog skillCatalog)
    {
        _skillCatalog = skillCatalog;
    }

    public UseSkillRequest? GetBestAction(
        ServerCombatant actor,
        IEnumerable<ServerCombatant> combatants,
        AutoBattlePolicy policy = AutoBattlePolicy.Balanced)
    {
        var validTargets = combatants.Where(c => c.Hp > 0).ToList();
        var enemies = validTargets.Where(c => c.Team != actor.Team).ToList();
        var allies = validTargets.Where(c => c.Team == actor.Team).ToList();

        if (enemies.Count == 0) return null;

        // 1. Survival: Heal (High Priority)
        if (policy != AutoBattlePolicy.Aggressive)
        {
            var lowHpAlly = allies
                .Where(a => (float)a.Hp / a.MaxHp < CriticalHpPercent)
                .OrderBy(a => a.Hp)
                .FirstOrDefault();

            if (lowHpAlly != null)
            {
                var healSkillId = FindBestSkill(actor, SkillEffectTag.Heal, SkillTargetType.SingleAlly, true);
                if (healSkillId.HasValue)
                {
                    return new UseSkillRequest
                    {
                        PlayerId = actor.Id,
                        SkillId = healSkillId.Value,
                        TargetId = lowHpAlly.Id
                    };
                }
            }
        }

        // 2. Support: Cleanse
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced)
        {
            var debuffedAlly = allies.FirstOrDefault(a =>
                a.StatusEffects.Any(e => e.Tag == SkillEffectTag.Seal || e.Tag == SkillEffectTag.DebuffStats ||
                                         e.Tag == SkillEffectTag.Burn));

            if (debuffedAlly != null)
            {
                var cleanseSkillId = FindBestSkill(actor, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly, true);
                if (cleanseSkillId.HasValue)
                {
                    return new UseSkillRequest
                    {
                        PlayerId = actor.Id,
                        SkillId = cleanseSkillId.Value,
                        TargetId = debuffedAlly.Id
                    };
                }
            }
        }

        // 3. Control: Dispel Enemy Buffs
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced ||
            policy == AutoBattlePolicy.Aggressive)
        {
            var buffedEnemy = enemies.FirstOrDefault(e =>
                e.StatusEffects.Any(s => s.Tag == SkillEffectTag.BuffStats || s.Tag == SkillEffectTag.Shield));

            if (buffedEnemy != null)
            {
                var dispelSkillId = FindBestSkill(actor, SkillEffectTag.Dispel, SkillTargetType.SingleEnemy, false);
                if (dispelSkillId.HasValue)
                {
                    return new UseSkillRequest
                    {
                        PlayerId = actor.Id,
                        SkillId = dispelSkillId.Value,
                        TargetId = buffedEnemy.Id
                    };
                }
            }
        }

        // 4. Control: Seal
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced ||
            policy == AutoBattlePolicy.Aggressive)
        {
            var targetEnemy = enemies
                .Where(e => !e.StatusEffects.Any(s => s.Tag == SkillEffectTag.Seal))
                .OrderByDescending(e => e.Atk + e.Mat) // Target strongest
                .FirstOrDefault();

            if (targetEnemy != null)
            {
                var sealSkillId = FindBestSkill(actor, SkillEffectTag.Seal, SkillTargetType.SingleEnemy, false);
                if (sealSkillId.HasValue)
                {
                    return new UseSkillRequest
                    {
                        PlayerId = actor.Id,
                        SkillId = sealSkillId.Value,
                        TargetId = targetEnemy.Id
                    };
                }
            }
        }

        // 5. Buffs
        if (policy == AutoBattlePolicy.Supportive || policy == AutoBattlePolicy.Balanced)
        {
            // Sort for determinism
            var sortedSkills = actor.SkillMastery.Keys.OrderBy(k => k).ToList();

            foreach (var skillId in sortedSkills)
            {
                var skill = _skillCatalog.GetSkillById(skillId);
                if (skill == null || actor.IsSkillOnCooldown(skill.SkillId) || actor.Sp < skill.SpCost)
                {
                    continue;
                }

                if (actor.Sp - skill.SpCost < MinSpThreshold)
                {
                    continue;
                }

                // Only consider SingleAlly buffs for now to keep it simple
                if (skill.TargetType != SkillTargetType.SingleAlly && skill.TargetType != SkillTargetType.AllAllies)
                {
                    continue;
                }

                var buffEffect = skill.Effects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats);
                if (buffEffect != null)
                {
                    var targetAlly = allies.FirstOrDefault(a => !HasConflictingBuff(a, buffEffect));

                    if (targetAlly != null)
                    {
                        return new UseSkillRequest
                        {
                            PlayerId = actor.Id,
                            SkillId = skill.SkillId,
                            TargetId = targetAlly.Id
                        };
                    }
                }
            }
        }

        // 6. Attack
        // Target selection: Weakest HP to secure kill
        var target = enemies.OrderBy(e => e.Hp).First();

        // Try to find a damage skill if SP permits
        if (actor.Sp > MinSpThreshold)
        {
            var damageSkillId = FindBestSkill(actor, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, false);
            if (damageSkillId.HasValue)
            {
                return new UseSkillRequest
                {
                    PlayerId = actor.Id,
                    SkillId = damageSkillId.Value,
                    TargetId = target.Id
                };
            }
        }

        // Fallback: Basic Attack (Any cheap damage skill, ignoring threshold)
        var basicAttack = FindBestSkill(actor, SkillEffectTag.Damage, SkillTargetType.SingleEnemy, true);
        if (basicAttack.HasValue)
        {
            return new UseSkillRequest
            {
                PlayerId = actor.Id,
                SkillId = basicAttack.Value,
                TargetId = target.Id
            };
        }

        return null;
    }

    private int? FindBestSkill(
        ServerCombatant actor,
        SkillEffectTag effectTag,
        SkillTargetType targetType,
        bool ignoreThreshold)
    {
        int? bestSkillId = null;
        var maxCost = -1;

        // Sort for determinism
        var sortedSkills = actor.SkillMastery.Keys.OrderBy(k => k).ToList();

        foreach (var skillId in sortedSkills)
        {
            var skill = _skillCatalog.GetSkillById(skillId);
            if (skill == null)
            {
                continue;
            }

            if (actor.IsSkillOnCooldown(skillId))
            {
                continue;
            }

            if (actor.Sp < skill.SpCost)
            {
                continue;
            }

            if (!ignoreThreshold && actor.Sp - skill.SpCost < MinSpThreshold)
            {
                continue;
            }

            // Check target type compatibility (loose check)
            var targetMatch = false;

            if (targetType == SkillTargetType.SingleAlly && (skill.TargetType == SkillTargetType.SingleAlly ||
                                                             skill.TargetType == SkillTargetType.AllAllies))
            {
                targetMatch = true;
            }
            else if (targetType == SkillTargetType.SingleEnemy &&
                     (skill.TargetType == SkillTargetType.SingleEnemy ||
                      skill.TargetType == SkillTargetType.AllEnemies ||
                      skill.TargetType == SkillTargetType.RowEnemies))
            {
                targetMatch = true;
            }

            if (targetMatch)
            {
                if (skill.Effects.Any(e => e.Tag == effectTag))
                {
                    // Pick the most expensive one (heuristic for power)
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

    private bool HasConflictingBuff(ServerCombatant combatant, SkillEffect newEffect)
    {
        if (!string.IsNullOrEmpty(newEffect.ConflictGroup))
        {
            return combatant.StatusEffects.Any(e => e.ConflictGroup == newEffect.ConflictGroup);
        }

        if (newEffect.Tag == SkillEffectTag.BuffStats && !string.IsNullOrEmpty(newEffect.Param))
        {
            return combatant.StatusEffects.Any(e => e.Tag == SkillEffectTag.BuffStats && e.Param == newEffect.Param);
        }

        return false;
    }
}
