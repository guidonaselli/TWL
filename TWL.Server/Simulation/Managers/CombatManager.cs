using System.Collections.Concurrent;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

/// <summary>
///     CombatManager vive en el servidor. Gestiona turnos y el cálculo de daño real.
/// </summary>
public class CombatManager
{
    private readonly ConcurrentDictionary<int, ServerCombatant> _combatants;
    private readonly ConcurrentDictionary<int, ITurnEngine> _encounters = new();
    private readonly IRandomService _random;
    private readonly ICombatResolver _resolver;
    private readonly ISkillCatalog _skills;
    private readonly IStatusEngine _statusEngine;

    public CombatManager(ICombatResolver resolver, IRandomService random, ISkillCatalog skills,
        IStatusEngine statusEngine)
    {
        _combatants = new ConcurrentDictionary<int, ServerCombatant>();
        _resolver = resolver;
        _random = random;
        _skills = skills;
        _statusEngine = statusEngine;
    }

    public event Action<ServerCombatant>? OnCombatantDeath;

    public virtual void RegisterCombatant(ServerCombatant combatant) => _combatants[combatant.Id] = combatant;

    public virtual void UnregisterCombatant(int id) => _combatants.TryRemove(id, out _);

    public virtual ServerCombatant? GetCombatant(int id)
    {
        _combatants.TryGetValue(id, out var combatant);
        return combatant;
    }

    public virtual void StartEncounter(int encounterId, IEnumerable<ServerCombatant> participants, int seed = 0)
    {
        foreach (var p in participants)
        {
            p.EncounterId = encounterId;
            RegisterCombatant(p);
        }

        var turnEngine = new TurnEngine(_random);
        turnEngine.StartEncounter(participants);
        _encounters[encounterId] = turnEngine;

        // Start first turn
        turnEngine.NextTurn();
    }

    public virtual void EndEncounter(int encounterId)
    {
        _encounters.TryRemove(encounterId, out _);
    }

    // Legacy / Convenience
    public void AddCharacter(ServerCharacter character) => RegisterCombatant(character);
    public void RemoveCharacter(int id) => UnregisterCombatant(id);
    public ServerCharacter? GetCharacter(int id) => GetCombatant(id) as ServerCharacter;
    public virtual List<ServerCharacter> GetAllCharacters() => _combatants.Values.OfType<ServerCharacter>().ToList();

    /// <summary>
    ///     Usa una skill (basado en la petición del cliente).
    /// </summary>
    public List<CombatResult> UseSkill(UseSkillRequest request)
    {
        // 1) Obtenemos los objetos server-side
        if (!_combatants.TryGetValue(request.PlayerId, out var attacker) ||
            !_combatants.TryGetValue(request.TargetId, out var primaryTarget))
        {
            return new List<CombatResult>();
        }

        var skill = _skills.GetSkillById(request.SkillId);
        if (skill == null)
        {
            return new List<CombatResult>();
        }

        ITurnEngine? turnEngine = null;
        if (_encounters.TryGetValue(attacker.EncounterId, out turnEngine))
        {
            if (turnEngine.CurrentCombatant?.Id != attacker.Id)
            {
                return new List<CombatResult>();
            }
        }

        if (attacker is ServerPet pet && !pet.CheckObedience(_random.NextFloat("PetObedience")))
        {
            if (turnEngine != null)
            {
                turnEngine.EndTurn();
                turnEngine.NextTurn();
            }

            return new List<CombatResult>
            {
                new CombatResult
                {
                    AttackerId = attacker.Id,
                    TargetId = primaryTarget.Id,
                    IsDisobey = true
                }
            };
        }

        if (attacker.IsSkillOnCooldown(skill.SkillId))
        {
            return new List<CombatResult>();
        }

        if (!attacker.ConsumeSp(skill.SpCost))
        {
            return new List<CombatResult>();
        }

        var targets = SkillTargetingHelper.GetTargets(skill, attacker, primaryTarget, GetAllCombatants());
        var results = new List<CombatResult>();

        foreach (var target in targets)
        {
            int newTargetHp;
            // 2) Apply Effects & Calculate Damage
            var appliedEffects = new List<StatusEffectInstance>();

            foreach (var effect in skill.Effects)
            {
                var chance = effect.Chance;

                if (skill.HitRules != null && (effect.Tag == SkillEffectTag.Seal || effect.Tag == SkillEffectTag.DebuffStats))
                {
                    chance = CalculateHitChance(attacker, target, skill.HitRules);
                }

                var resist = false;
                var finalDuration = effect.Duration;
                var finalValue = effect.Value;

                if (effect.ResistanceTags != null && effect.ResistanceTags.Count > 0)
                {
                    foreach (var tag in effect.ResistanceTags)
                    {
                        var resistance = target.GetResistance(tag);

                        // Immunity Check
                        if (resistance >= 1.0f)
                        {
                            resist = true;
                            break;
                        }

                        // Resistance Roll
                        if (_random.NextFloat("ResistanceRoll") < resistance)
                        {
                            if (effect.Outcome == OutcomeModel.Partial)
                            {
                                finalDuration = Math.Max(1, finalDuration / 2);
                                finalValue *= 0.5f;
                            }
                            else
                            {
                                // OutcomeModel.Resist (or default)
                                resist = true;
                                break;
                            }
                        }
                    }
                }

                if (!resist && _random.NextFloat("EffectChance") <= chance)
                {
                    switch (effect.Tag)
                    {
                        case SkillEffectTag.Cleanse:
                            target.CleanseDebuffs(_statusEngine);
                            break;
                        case SkillEffectTag.Dispel:
                            target.DispelBuffs(_statusEngine);
                            break;
                        case SkillEffectTag.Damage:
                            break;
                        case SkillEffectTag.Heal:
                            var healAmount = _resolver.CalculateHeal(attacker, target, request);
                            healAmount += (int)finalValue;
                            target.Heal(healAmount);
                            break;
                        default:
                            var status = new StatusEffectInstance(effect.Tag, finalValue, finalDuration, effect.Param)
                            {
                                SourceSkillId = skill.SkillId,
                                StackingPolicy = effect.StackingPolicy,
                                MaxStacks = effect.MaxStacks,
                                Priority = effect.Priority,
                                ConflictGroup = effect.ConflictGroup
                            };
                            target.AddStatusEffect(status, _statusEngine);
                            appliedEffects.Add(status);
                            break;
                    }
                }
            }

            var finalDamage = _resolver.CalculateDamage(attacker, target, request);
            newTargetHp = target.ApplyDamage(finalDamage);

            if (finalDamage > 0)
            {
                target.LastAttackerId = attacker.Id;
            }

            // Check for death
            if (newTargetHp <= 0)
            {
                turnEngine?.RemoveCombatant(target.Id);
                OnCombatantDeath?.Invoke(target);
            }

            results.Add(new CombatResult
            {
                AttackerId = attacker.Id,
                TargetId = target.Id,
                Damage = finalDamage,
                NewTargetHp = newTargetHp,
                AddedEffects = appliedEffects,
                TargetDied = newTargetHp <= 0
            });
        }

        attacker.IncrementSkillUsage(skill.SkillId);
        attacker.SetSkillCooldown(skill.SkillId, skill.Cooldown);
        CheckSkillEvolution(attacker, skill);

        if (turnEngine != null)
        {
            turnEngine.EndTurn();
            turnEngine.NextTurn();
        }

        return results;
    }

    private void CheckSkillEvolution(ServerCombatant combatant, Skill skill)
    {
        if (skill.StageUpgradeRules == null)
        {
            return;
        }

        if (combatant.SkillMastery.TryGetValue(skill.SkillId, out var mastery))
        {
            if (mastery.Rank >= skill.StageUpgradeRules.RankThreshold)
            {
                if (skill.StageUpgradeRules.NextSkillId.HasValue)
                {
                    combatant.ReplaceSkill(skill.SkillId, skill.StageUpgradeRules.NextSkillId.Value);
                }
            }
        }
    }

    private float CalculateHitChance(ServerCombatant attacker, ServerCombatant target, SkillHitRules rules)
    {
        var chance = rules.BaseChance;

        if (!string.IsNullOrEmpty(rules.StatDependence))
        {
            var parts = rules.StatDependence.Split('-');
            if (parts.Length > 0)
            {
                var stat1 = GetStatValue(attacker, parts[0]);
                var stat2 = parts.Length > 1 ? GetStatValue(target, parts[1]) : 0;

                chance += (stat1 - stat2) * 0.01f;
            }
        }

        if (chance < rules.MinChance)
        {
            chance = rules.MinChance;
        }

        if (chance > rules.MaxChance)
        {
            chance = rules.MaxChance;
        }

        return chance;
    }

    private int GetStatValue(ServerCombatant combatant, string statName)
    {
        return statName.ToLowerInvariant() switch
        {
            "str" => combatant.Str,
            "con" => combatant.Con,
            "int" => combatant.Int,
            "wis" => combatant.Wis,
            "agi" => combatant.Agi,
            "atk" => combatant.Atk,
            "def" => combatant.Def,
            "mat" => combatant.Mat,
            "mdf" => combatant.Mdf,
            "spd" => combatant.Spd,
            _ => 0
        };
    }

    public virtual List<ServerCombatant> GetAllCombatants() => _combatants.Values.ToList();
}