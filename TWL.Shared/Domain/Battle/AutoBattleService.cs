using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Shared.Domain.Battle;

public class AutoBattleService
{
    private readonly ISkillCatalog _skillCatalog;

    public int MinSpThreshold { get; set; } = 10;
    public float CriticalHpPercent { get; set; } = 0.3f;

    public AutoBattleService(ISkillCatalog skillCatalog)
    {
        _skillCatalog = skillCatalog;
    }

    public UseSkillRequest? GetBestAction(
        ICombatant actor,
        IEnumerable<ICombatant> combatants,
        AutoBattlePolicy policy = AutoBattlePolicy.Balanced,
        IRandomService? random = null)
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
                .ThenBy(a => a.Id)
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
                var cleanseSkillId = FindBestSkill(actor, SkillEffectTag.Cleanse, SkillTargetType.SingleAlly, true, skill =>
                {
                    var effect = skill.Effects.First(e => e.Tag == SkillEffectTag.Cleanse);
                    if (effect.ResistanceTags == null || effect.ResistanceTags.Count == 0) return true;
                    // Check if ally has ANY debuff that matches the allowed tags
                    return debuffedAlly.StatusEffects.Any(s => effect.ResistanceTags.Contains(s.ConflictGroup) || effect.ResistanceTags.Contains(s.Tag.ToString()));
                });

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
                var dispelSkillId = FindBestSkill(actor, SkillEffectTag.Dispel, SkillTargetType.SingleEnemy, false, skill =>
                {
                    var effect = skill.Effects.First(e => e.Tag == SkillEffectTag.Dispel);
                    if (effect.ResistanceTags == null || effect.ResistanceTags.Count == 0) return true;
                    // Check if enemy has ANY buff that matches the allowed tags
                    return buffedEnemy.StatusEffects.Any(s => effect.ResistanceTags.Contains(s.ConflictGroup) || effect.ResistanceTags.Contains(s.Tag.ToString()));
                });

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
                .Where(e => !e.StatusEffects.Any(s => s.Tag == SkillEffectTag.Seal) && e.GetResistance("SealResist") < 1.0f)
                .OrderByDescending(e => e.Atk + e.Mat) // Target strongest
                .ThenBy(e => e.Id)
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
            var sortedSkills = actor.GetKnownSkillIds().OrderBy(k => k).ToList();

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
                    // Filter allies who don't have the buff
                    var candidates = allies.Where(a => !HasConflictingBuff(a, buffEffect)).ToList();

                    if (candidates.Any())
                    {
                        // Heuristic: Pick the ally who benefits most from the stat
                        var targetAlly = candidates.First(); // Default

                        if (!string.IsNullOrEmpty(buffEffect.Param))
                        {
                            switch (buffEffect.Param)
                            {
                                case "Atk":
                                    targetAlly = candidates.OrderByDescending(c => c.Atk).First();
                                    break;
                                case "Mat":
                                    targetAlly = candidates.OrderByDescending(c => c.Mat).First();
                                    break;
                                case "Def":
                                    // Maybe tank? High Def or Low Def? Usually buff tank (High Def) or weakling?
                                    // Let's go with High Def as "Tank" role heuristic
                                    targetAlly = candidates.OrderByDescending(c => c.Def).First();
                                    break;
                                case "Mdf":
                                    targetAlly = candidates.OrderByDescending(c => c.Mdf).First();
                                    break;
                                case "Spd":
                                    targetAlly = candidates.OrderByDescending(c => c.Spd).First();
                                    break;
                                // Add more as needed
                            }
                        }

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
        // Target selection: Elemental Advantage > Weakest HP
        var target = enemies
            .OrderByDescending(e => GetElementAdvantage(actor.CharacterElement, e.CharacterElement)) // Prefer 1.5x targets
            .ThenBy(e => e.Hp) // Then pick the weakest among them
            .ThenBy(e => e.Id) // Deterministic tie-break
            .First();

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

    private float GetElementAdvantage(Element attacker, Element defender)
    {
        if (attacker == Element.Earth && defender == Element.Water) return 1.5f;
        if (attacker == Element.Water && defender == Element.Fire) return 1.5f;
        if (attacker == Element.Fire && defender == Element.Wind) return 1.5f;
        if (attacker == Element.Wind && defender == Element.Earth) return 1.5f;

        if (attacker == Element.Water && defender == Element.Earth) return 0.5f;
        if (attacker == Element.Fire && defender == Element.Water) return 0.5f;
        if (attacker == Element.Wind && defender == Element.Fire) return 0.5f;
        if (attacker == Element.Earth && defender == Element.Wind) return 0.5f;

        return 1.0f;
    }

    private int? FindBestSkill(
        ICombatant actor,
        SkillEffectTag effectTag,
        SkillTargetType targetType,
        bool ignoreThreshold,
        Predicate<Skill>? extraFilter = null)
    {
        int? bestSkillId = null;
        var maxCost = -1;

        // Sort for determinism
        var sortedSkills = actor.GetKnownSkillIds().OrderBy(k => k).ToList();

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
                    if (extraFilter != null && !extraFilter(skill))
                    {
                        continue;
                    }

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

    private bool HasConflictingBuff(ICombatant combatant, SkillEffect newEffect)
    {
        // 1. Check Conflict Groups
        if (!string.IsNullOrEmpty(newEffect.ConflictGroup))
        {
            var conflict = combatant.StatusEffects.FirstOrDefault(e => e.ConflictGroup == newEffect.ConflictGroup);
            if (conflict != null)
            {
                // Check if it's the same effect (candidate for Stacking/Refresh)
                bool isSameEffect = conflict.Tag == newEffect.Tag &&
                                    string.Equals(conflict.Param, newEffect.Param, StringComparison.OrdinalIgnoreCase);

                if (isSameEffect && (newEffect.StackingPolicy == StackingPolicy.StackUpToN ||
                                     newEffect.StackingPolicy == StackingPolicy.RefreshDuration))
                {
                    return false; // Allow stacking or refreshing
                }

                // If priority allows overwriting
                // Only overwrite if STRICTLY stronger to avoid wasting turns on same-priority recasts
                // unless the policy allows refresh (handled above).
                if (newEffect.Priority > conflict.Priority)
                {
                    return false;
                }

                // If priority is equal, and policy is NoStackOverwrite, we usually block to avoid spam.
                // StatusEngine allows overwrite on equal, but AutoBattle should be conservative.

                return true; // Conflict prevents casting
            }
        }

        // 2. Check Tag/Param (Implicit conflict if same type)
        if (newEffect.Tag == SkillEffectTag.BuffStats && !string.IsNullOrEmpty(newEffect.Param))
        {
            var existing = combatant.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.BuffStats && e.Param == newEffect.Param);
            if (existing != null)
            {
                // RefreshDuration or StackUpToN means we CAN cast (to refresh/stack)
                if (newEffect.StackingPolicy == StackingPolicy.RefreshDuration || newEffect.StackingPolicy == StackingPolicy.StackUpToN)
                {
                    return false;
                }

                // NoStackOverwrite
                if (newEffect.StackingPolicy == StackingPolicy.NoStackOverwrite)
                {
                     // Check Priority
                     if (newEffect.Priority > existing.Priority)
                     {
                         return false;
                     }
                     return true;
                }

                return true;
            }
        }

        return false;
    }
}
