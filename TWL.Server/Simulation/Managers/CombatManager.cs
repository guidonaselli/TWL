using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

// donde tienes CombatResult, UseSkillRequest, etc.

namespace TWL.Server.Simulation.Managers;

/// <summary>
///     CombatManager vive en el servidor. Gestiona turnos y el cálculo de daño real.
/// </summary>
public class CombatManager
{
    // Supongamos que guardamos todos los personajes en un diccionario
    // (en un MMO real podrías tener combates instanciados).
    private readonly ConcurrentDictionary<int, ServerCharacter> _characters;
    private readonly ICombatResolver _resolver;
    private readonly IRandomService _random;

    public CombatManager(ICombatResolver resolver, IRandomService random)
    {
        _characters = new ConcurrentDictionary<int, ServerCharacter>();
        _resolver = resolver;
        _random = random;
    }

    public void AddCharacter(ServerCharacter character)
    {
        _characters[character.Id] = character;
    }

    /// <summary>
    ///     Usa una skill (basado en la petición del cliente).
    /// </summary>
    public CombatResult UseSkill(UseSkillRequest request)
    {
        // 1) Obtenemos los objetos server-side
        if (!_characters.TryGetValue(request.PlayerId, out var attacker) ||
            !_characters.TryGetValue(request.TargetId, out var target))
            // En un caso real, podrías retornar un error o un CombatResult con "invalid target".
            return null;

        var skill = SkillRegistry.Instance.GetSkillById(request.SkillId);
        if (skill == null) return null;

        if (!attacker.ConsumeSp(skill.SpCost))
        {
            return null;
        }

        int newTargetHp;
        // 2) Apply Effects & Calculate Damage
        // Check for control hit rules if applicable
        var appliedEffects = new List<TWL.Shared.Domain.Battle.StatusEffectInstance>();

        foreach (var effect in skill.Effects)
        {
            // Simple logic: if chance met, apply
            float chance = effect.Chance;

            // Override chance if HitRules exist and tag is Control/Seal
            if (skill.HitRules != null && effect.Tag == SkillEffectTag.Seal)
            {
                // Basic formula: Base + (Int - Wis)*0.01 (clamped)
                // Assuming StatDependence is "Int-Wis"
                float statDiff = (attacker.Int - target.Wis) * 0.01f;
                chance = skill.HitRules.BaseChance + statDiff;
                if (chance < skill.HitRules.MinChance) chance = skill.HitRules.MinChance;
                if (chance > skill.HitRules.MaxChance) chance = skill.HitRules.MaxChance;
            }

            if (_random.NextFloat() <= chance)
            {
                // Apply specific logic
                switch (effect.Tag)
                {
                    case SkillEffectTag.Cleanse:
                        target.CleanseDebuffs();
                        break;
                    case SkillEffectTag.Dispel:
                        target.DispelBuffs();
                        break;
                    case SkillEffectTag.Damage:
                        // Handled by resolver usually, but if it's flat damage or secondary, we might do it here.
                        // Standard resolver handles primary damage scaling.
                        break;
                    default:
                        // Add status
                        var status = new TWL.Shared.Domain.Battle.StatusEffectInstance(effect.Tag, effect.Value, effect.Duration, effect.Param);
                        target.AddStatusEffect(status);
                        appliedEffects.Add(status);
                        break;
                }
            }
        }

        int finalDamage = _resolver.CalculateDamage(attacker, target, request);
        newTargetHp = target.ApplyDamage(finalDamage);

        attacker.IncrementSkillUsage(skill.SkillId);
        CheckSkillEvolution(attacker, skill);

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

    private void CheckSkillEvolution(ServerCharacter character, Skill skill)
    {
        if (skill.StageUpgradeRules == null) return;

        if (character.SkillMastery.TryGetValue(skill.SkillId, out var mastery))
        {
            if (mastery.Rank >= skill.StageUpgradeRules.RankThreshold)
            {
                if (skill.StageUpgradeRules.NextSkillId.HasValue)
                {
                    character.ReplaceSkill(skill.SkillId, skill.StageUpgradeRules.NextSkillId.Value);
                }
            }
        }
    }

    // Ejemplo: Lógica de turnos (opcional). Podrías llevar un "battleId" y states.
    // public void NextTurn(int battleId) { ... }

    // Podrías agregar más métodos: Revive, ApplyBuff, etc.

    public void RemoveCharacter(int id)
    {
        _characters.TryRemove(id, out _);
    }

    public ServerCharacter? GetCharacter(int id)
    {
        _characters.TryGetValue(id, out var character);
        return character;
    }

    public List<ServerCharacter> GetAllCharacters()
    {
        return _characters.Values.ToList();
    }
}