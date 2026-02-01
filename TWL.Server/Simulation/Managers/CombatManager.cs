using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ICombatResolver _resolver;
    private readonly IRandomService _random;
    private readonly ISkillCatalog _skills;
    private readonly IStatusEngine _statusEngine;

    public event Action<ServerCombatant>? OnCombatantDeath;

    public CombatManager(ICombatResolver resolver, IRandomService random, ISkillCatalog skills, IStatusEngine statusEngine)
    {
        _combatants = new ConcurrentDictionary<int, ServerCombatant>();
        _resolver = resolver;
        _random = random;
        _skills = skills;
        _statusEngine = statusEngine;
    }

    public virtual void RegisterCombatant(ServerCombatant combatant)
    {
        _combatants[combatant.Id] = combatant;
    }

    public virtual void UnregisterCombatant(int id)
    {
        _combatants.TryRemove(id, out _);
    }

    public virtual ServerCombatant? GetCombatant(int id)
    {
        _combatants.TryGetValue(id, out var combatant);
        return combatant;
    }

    public virtual void StartEncounter(int encounterId, List<ServerCharacter> participants, int seed = 0)
    {
        foreach (var p in participants)
        {
            RegisterCombatant(p);
        }
        // Seed can be used to initialize determinstic RNG for this encounter
    }

    // Legacy / Convenience
    public void AddCharacter(ServerCharacter character) => RegisterCombatant(character);
    public void RemoveCharacter(int id) => UnregisterCombatant(id);
    public ServerCharacter? GetCharacter(int id) => GetCombatant(id) as ServerCharacter;
    public List<ServerCharacter> GetAllCharacters() => _combatants.Values.OfType<ServerCharacter>().ToList();

    /// <summary>
    ///     Usa una skill (basado en la petición del cliente).
    /// </summary>
    public CombatResult UseSkill(UseSkillRequest request)
    {
        // 1) Obtenemos los objetos server-side
        if (!_combatants.TryGetValue(request.PlayerId, out var attacker) ||
            !_combatants.TryGetValue(request.TargetId, out var target))
            return null;

        var skill = _skills.GetSkillById(request.SkillId);
        if (skill == null) return null;

        if (attacker.IsSkillOnCooldown(skill.SkillId))
        {
            return null;
        }

        if (!attacker.ConsumeSp(skill.SpCost))
        {
            return null;
        }

        int newTargetHp;
        // 2) Apply Effects & Calculate Damage
        var appliedEffects = new List<StatusEffectInstance>();

        foreach (var effect in skill.Effects)
        {
            float chance = effect.Chance;

            if (skill.HitRules != null && effect.Tag == SkillEffectTag.Seal)
            {
                // Basic formula: Base + (Int - Wis)*0.01 (clamped)
                float statDiff = (attacker.Int - target.Wis) * 0.01f;
                chance = skill.HitRules.BaseChance + statDiff;
                if (chance < skill.HitRules.MinChance) chance = skill.HitRules.MinChance;
                if (chance > skill.HitRules.MaxChance) chance = skill.HitRules.MaxChance;
            }

            bool resist = false;
            int finalDuration = effect.Duration;
            float finalValue = effect.Value;

            if (effect.ResistanceTags != null && effect.ResistanceTags.Count > 0)
            {
                foreach (var tag in effect.ResistanceTags)
                {
                    float resistance = target.GetResistance(tag);

                    // Immunity Check
                    if (resistance >= 1.0f)
                    {
                        resist = true;
                        break;
                    }

                    // Resistance Roll
                    if (_random.NextFloat() < resistance)
                    {
                        if (effect.Outcome == OutcomeModel.Partial)
                        {
                            finalDuration = System.Math.Max(1, finalDuration / 2);
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

            if (!resist && _random.NextFloat() <= chance)
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
                        int healAmount = _resolver.CalculateHeal(attacker, target, request);
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

        int finalDamage = _resolver.CalculateDamage(attacker, target, request);
        newTargetHp = target.ApplyDamage(finalDamage);

        if (finalDamage > 0)
        {
            target.LastAttackerId = attacker.Id;
        }

        attacker.IncrementSkillUsage(skill.SkillId);
        attacker.SetSkillCooldown(skill.SkillId, skill.Cooldown);
        CheckSkillEvolution(attacker, skill);

        // Check for death
        if (newTargetHp <= 0)
        {
            OnCombatantDeath?.Invoke(target);
        }

        // 3) Retornar el resultado para avisar al cliente.
        var result = new CombatResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Damage = finalDamage,
            NewTargetHp = newTargetHp,
            AddedEffects = appliedEffects
        };

        return result;
    }

    private void CheckSkillEvolution(ServerCombatant combatant, Skill skill)
    {
        if (skill.StageUpgradeRules == null) return;

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

    public List<ServerCombatant> GetAllCombatants()
    {
        return _combatants.Values.ToList();
    }
}
