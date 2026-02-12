using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Shared.Domain.Battle;

public enum BattleState
{
    Active,
    Victory,
    Defeat
}

public class BattleInstance
{
    // Queue of combatants who are ready to act (ATB >= 100)
    private readonly Queue<Combatant> _readyQueue = new();

    // Dependency on Skill Catalog
    private readonly ISkillCatalog _skillCatalog;
    private readonly IRandomService _random;

    public BattleInstance(IEnumerable<Character> allies, IEnumerable<Character> enemies,
        ISkillCatalog? skillCatalog = null, IRandomService? random = null)
    {
        var idCounter = 1;
        Allies = allies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
        Enemies = enemies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
        _skillCatalog = skillCatalog ?? SkillRegistry.Instance;
        _random = random ?? new DefaultRandomService();
    }

    public List<Combatant> Allies { get; }
    public List<Combatant> Enemies { get; }
    public List<Combatant> AllCombatants => Allies.Concat(Enemies).ToList();

    public BattleState State { get; private set; } = BattleState.Active;

    // Current combatant whose turn it is
    public Combatant CurrentTurnCombatant { get; private set; }

    public event Action<int, int> OnSkillUsed;

    public void Tick(float deltaTimeSeconds)
    {
        if (State != BattleState.Active)
        {
            return;
        }

        if (CurrentTurnCombatant != null)
        {
            return; // Waiting for action
        }

        // If no one is waiting for input, fill ATB
        if (_readyQueue.Count == 0)
        {
            foreach (var c in AllCombatants)
            {
                if (!c.Character.IsAlive())
                {
                    c.Atb = 0;
                    continue;
                }

                // ATB Fill Speed formula: Base + Spd * Factor
                double fillRate = (10 + c.Character.Spd) * 5; // Increased speed for snappier combat
                c.Atb += fillRate * deltaTimeSeconds;

                if (c.Atb >= 100)
                {
                    c.Atb = 100;
                    if (!_readyQueue.Contains(c))
                    {
                        _readyQueue.Enqueue(c);
                    }
                }
            }
        }

        if (_readyQueue.Count > 0 && CurrentTurnCombatant == null)
        {
            CurrentTurnCombatant = _readyQueue.Dequeue();
            CurrentTurnCombatant.IsDefending = false;
        }
    }

    public string ResolveAction(CombatAction action)
    {
        if (CurrentTurnCombatant == null)
        {
            return "No active turn";
        }

        if (CurrentTurnCombatant.BattleId != action.ActorId)
        {
            return "Not your turn";
        }

        var actor = CurrentTurnCombatant;

        // Check for Seal/Control
        if (actor.StatusEffects.Any(e => e.Tag == SkillEffectTag.Seal))
        {
            actor.Atb = 0;
            CurrentTurnCombatant = null;
            return $"{actor.Character.Name} is sealed and cannot act!";
        }

        var targetCombatant = AllCombatants.FirstOrDefault(c => c.BattleId == action.TargetId);

        var resultMessage = "";

        switch (action.Type)
        {
            case CombatActionType.Attack:
                if (targetCombatant != null)
                {
                    var dmgVal = actor.Character.CalculatePhysicalDamage();
                    if (actor.AttackBuffTurns > 0)
                    {
                        dmgVal = (int)(dmgVal * 1.5);
                    }

                    var defense = GetEffectiveStat(targetCombatant, StatType.Def);
                    var damage = Math.Max(1, dmgVal - defense);
                    if (targetCombatant.IsDefending)
                    {
                        damage /= 2;
                    }

                    damage = ApplyDamage(targetCombatant, damage);
                    resultMessage =
                        $"{actor.Character.Name} attacks {targetCombatant.Character.Name} for {damage} damage!";
                }
                else
                {
                    resultMessage = $"{actor.Character.Name} attacks thin air!";
                }

                break;

            case CombatActionType.Defend:
                actor.IsDefending = true;
                resultMessage = $"{actor.Character.Name} defends!";
                break;

            case CombatActionType.Skill:
                resultMessage = UseSkill(actor, targetCombatant, action.SkillId);
                if (resultMessage == "Not enough SP!" || resultMessage == "No target")
                {
                    return resultMessage;
                }

                break;

            case CombatActionType.Flee:
                resultMessage = $"{actor.Character.Name} tries to flee... failed!";
                break;
        }

        // End turn
        if (actor.AttackBuffTurns > 0)
        {
            actor.AttackBuffTurns--;
        }

        // Process Status Effects (Burn, etc)
        var statusLogs = ProcessStatusEffects(actor);
        if (!string.IsNullOrEmpty(statusLogs))
        {
            resultMessage += " " + statusLogs;
        }

        actor.Atb = 0;
        CurrentTurnCombatant = null;

        CheckBattleEnd();

        return resultMessage;
    }

    private string UseSkill(Combatant actor, Combatant target, int skillId)
    {
        var skill = _skillCatalog.GetSkillById(skillId);

        // Legacy fallback
        if (skill == null)
        {
            return UseLegacySkill(actor, target, skillId);
        }

        if (!actor.Character.ConsumeSp(skill.SpCost))
        {
            return "Not enough SP!";
        }

        if (target == null && skill.TargetType == SkillTargetType.SingleEnemy)
        {
            return "No target";
        }

        // Logic for Data-Driven Skill
        return ApplyDataDrivenSkill(actor, target, skill);
    }

    private string ApplyDataDrivenSkill(Combatant actor, Combatant target, Skill skill)
    {
        // Identify Targets
        var targets = new List<Combatant>();

        if (skill.TargetType == SkillTargetType.RowEnemies && target != null)
        {
            // Simple logic: hit all enemies for now, or we would need row logic
            // Assuming "Row" implies multiple targets. For simplicity in this thin slice, we hit all enemies if Row.
            // A proper implementation would check Grid position.
            targets.AddRange(Enemies.Where(e => e.Character.IsAlive()));
        }
        else if (target != null)
        {
            targets.Add(target);
        }
        else if (skill.TargetType == SkillTargetType.Self)
        {
            targets.Add(actor);
        }

        float totalValue = 0;
        var didDamage = false;
        var didHeal = false;
        var lastDamage = 0;
        Combatant lastTarget = null;

        // Calculate base power from Scaling
        foreach (var scaling in skill.Scaling)
        {
            var statValue = GetStatValue(actor.Character, scaling.Stat);
            totalValue += statValue * scaling.Coefficient;
        }

        // Apply Buffs/Multipliers (e.g. AttackBuff) - simplified
        if (skill.Branch == SkillBranch.Physical && actor.AttackBuffTurns > 0)
        {
            totalValue *= 1.5f;
        }

        // Apply Effects to All Targets
        foreach (var currentTarget in targets)
        {
            foreach (var effect in skill.Effects)
            {
                if (effect.Tag == SkillEffectTag.Damage)
                {
                    var elemMult = GetElementalMultiplier(skill.Element, currentTarget.Character.CharacterElement);
                    var adjustedValue = totalValue * elemMult;

                    var defense = skill.Branch == SkillBranch.Magical
                        ? GetEffectiveStat(currentTarget, StatType.Mdf)
                        : GetEffectiveStat(currentTarget, StatType.Def);

                    var damage = Math.Max(1, (int)adjustedValue - defense);
                    if (currentTarget.IsDefending)
                    {
                        damage /= 2;
                    }

                    damage = ApplyDamage(currentTarget, damage);
                    didDamage = true;
                    lastDamage = damage;
                    lastTarget = currentTarget;
                }
                else if (effect.Tag == SkillEffectTag.Shield)
                {
                    var value = effect.Value;
                    if (value == 0 && totalValue > 0)
                    {
                        value = totalValue; // Use scaled value if provided
                    }

                    currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, value, effect.Duration,
                        effect.Param));
                    didHeal = true; // Treating shield as positive
                }
                else if (effect.Tag == SkillEffectTag.Heal)
                {
                    var healAmount = (int)totalValue;
                    if (healAmount == 0)
                    {
                        healAmount = (int)effect.Value;
                    }

                    currentTarget.Character.Heal(healAmount);
                    didHeal = true;
                }
                else if (effect.Tag == SkillEffectTag.BuffStats)
                {
                    if (_random.NextDouble("SkillEffect_BuffStats") <= effect.Chance)
                    {
                        var value = effect.Value;
                        // If value is 0 but we have scaling, use the calculated totalValue (for dynamic buffs)
                        if (value == 0 && totalValue > 0)
                        {
                            value = totalValue;
                        }

                        currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, value, effect.Duration,
                            effect.Param));
                    }
                }
                else if (effect.Tag == SkillEffectTag.DebuffStats)
                {
                    var hitChance = GetControlHitChance(actor.Character, currentTarget.Character, effect.Chance,
                        skill.HitRules);
                    if (_random.NextDouble("SkillEffect_DebuffStats") <= hitChance)
                    {
                        currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, effect.Value,
                            effect.Duration, effect.Param));
                    }
                }
                else if (effect.Tag == SkillEffectTag.Cleanse)
                {
                    var negativeTags = new[] { SkillEffectTag.Burn, SkillEffectTag.DebuffStats, SkillEffectTag.Seal };
                    foreach (var tag in negativeTags)
                    {
                        if (currentTarget.StatusEffects.Any(e => e.Tag == tag))
                        {
                            currentTarget.RemoveStatusEffect(tag);
                            didHeal = true; // Treating cleanse as a "positive" outcome for messaging
                        }
                    }
                }
                else if (effect.Tag == SkillEffectTag.Dispel)
                {
                    var positiveTags = new[] { SkillEffectTag.BuffStats, SkillEffectTag.Shield, SkillEffectTag.Heal };

                    var toRemove = currentTarget.StatusEffects
                        .Where(e => e.Tag == SkillEffectTag.BuffStats || e.Tag == SkillEffectTag.Shield)
                        .ToList();

                    foreach (var eff in toRemove)
                    {
                        currentTarget.StatusEffects.Remove(eff);
                        didHeal = true; // Treating dispel as a significant event
                    }
                }
                else if (effect.Tag == SkillEffectTag.Seal)
                {
                    var hitChance = GetControlHitChance(actor.Character, currentTarget.Character, effect.Chance,
                        skill.HitRules);
                    if (_random.NextDouble("SkillEffect_Seal") <= hitChance)
                    {
                        currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, effect.Value,
                            effect.Duration, effect.Param));
                        didDamage = true; // Treating seal application as offensive success
                    }
                }
                else if (effect.Tag == SkillEffectTag.Burn)
                {
                    if (_random.NextDouble("SkillEffect_Burn") <= effect.Chance)
                    {
                        currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, effect.Value,
                            effect.Duration, effect.Param));
                    }
                }
            }
        }

        if (didDamage)
        {
            OnSkillUsed?.Invoke(actor.Character.Id, skill.SkillId);
            if (lastTarget != null)
            {
                return $"{actor.Character.Name} uses {skill.Name} on {lastTarget.Character.Name} for {lastDamage}!";
            }

            return $"{actor.Character.Name} uses {skill.Name}!";
        }

        if (didHeal)
        {
            OnSkillUsed?.Invoke(actor.Character.Id, skill.SkillId);
            return $"{actor.Character.Name} uses {skill.Name} and heals!";
        }

        OnSkillUsed?.Invoke(actor.Character.Id, skill.SkillId);
        return $"{actor.Character.Name} uses {skill.Name}!";
    }

    public string ProcessStatusEffects(Combatant combatant)
    {
        var logs = new List<string>();
        // Process DoTs and Duration
        for (var i = combatant.StatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = combatant.StatusEffects[i];

            if (effect.Tag == SkillEffectTag.Burn)
            {
                var dmg = (int)effect.Value;
                combatant.Character.TakeDamage(dmg);
                logs.Add($"{combatant.Character.Name} takes {dmg} burn damage!");
            }

            effect.TurnsRemaining--;
            if (effect.TurnsRemaining <= 0)
            {
                combatant.StatusEffects.RemoveAt(i);
                logs.Add($"{combatant.Character.Name}'s {effect.Tag} wore off.");
            }
        }

        return string.Join(" ", logs);
    }

    private float GetStatValue(Character c, StatType stat)
    {
        switch (stat)
        {
            case StatType.Str: return c.Str;
            case StatType.Con: return c.Con;
            case StatType.Int: return c.Int;
            case StatType.Wis: return c.Wis;
            case StatType.Agi: return c.Agi;
            case StatType.Atk: return c.Atk;
            case StatType.Def: return c.Def;
            case StatType.Mat: return c.Mat;
            case StatType.Mdf: return c.Mdf;
            case StatType.Spd: return c.Spd;
            default: return 0;
        }
    }

    private int GetEffectiveStat(Combatant c, StatType stat)
    {
        float baseVal = 0;
        // Use Character calculation for derived stats where possible to include base logic
        if (stat == StatType.Def)
        {
            baseVal = c.Character.CalculateDefense();
        }
        else if (stat == StatType.Mdf)
        {
            baseVal = c.Character.CalculateMagicalDefense();
        }
        else
        {
            baseVal = GetStatValue(c.Character, stat);
        }

        var statName = stat.ToString();

        foreach (var effect in c.StatusEffects)
        {
            if (effect.Tag == SkillEffectTag.BuffStats && effect.Param == statName)
            {
                baseVal += effect.Value;
            }

            if (effect.Tag == SkillEffectTag.DebuffStats && effect.Param == statName)
            {
                baseVal -= effect.Value;
            }
        }

        return (int)baseVal;
    }

    private string UseLegacySkill(Combatant actor, Combatant target, int skillId)
    {
        var cost = 0;
        switch (skillId)
        {
            case 1: cost = 5; break; // Power Strike
            case 2: cost = 10; break; // Fireball
            case 3: cost = 15; break; // Heal
            case 4: cost = 10; break; // Focus (Buff)
        }

        if (!actor.Character.ConsumeSp(cost))
        {
            return "Not enough SP!";
        }

        if (target == null && skillId != 3 && skillId != 4)
        {
            return "No target"; // Heal/Focus might be self
        }

        switch (skillId)
        {
            case 1: // Power Strike (Phys)
                var baseDmg = (int)(actor.Character.CalculatePhysicalDamage() * 1.5);
                if (actor.AttackBuffTurns > 0)
                {
                    baseDmg = (int)(baseDmg * 1.5);
                }

                var dmg1 = Math.Max(1, baseDmg - target.Character.CalculateDefense());
                if (target.IsDefending)
                {
                    dmg1 /= 2;
                }

                dmg1 = ApplyDamage(target, dmg1);
                return $"{actor.Character.Name} uses Power Strike on {target.Character.Name} for {dmg1}!";

            case 2: // Fireball (Magic)
                // Magic not affected by physical attack buff
                var dmg2 = Math.Max(1,
                    actor.Character.CalculateMagicalDamage() * 2 - target.Character.CalculateMagicalDefense());
                dmg2 = ApplyDamage(target, dmg2);
                return $"{actor.Character.Name} casts Fireball on {target.Character.Name} for {dmg2}!";

            case 3: // Heal
                var heal = actor.Character.Int * 4;
                if (target == null)
                {
                    target = actor;
                }

                target.Character.Heal(heal);
                return $"{actor.Character.Name} heals {target.Character.Name} for {heal}!";

            case 4: // Focus
                target.AttackBuffTurns = 3;
                return $"{actor.Character.Name} focuses on {target.Character.Name}! Attack UP!";

            default:
                return $"{actor.Character.Name} uses unknown skill!";
        }
    }

    private void CheckBattleEnd()
    {
        if (Allies.All(a => !a.Character.IsAlive()))
        {
            State = BattleState.Defeat;
        }
        else if (Enemies.All(e => !e.Character.IsAlive()))
        {
            State = BattleState.Victory;
        }
    }

    public void ForceEnd() => State = BattleState.Defeat;

    private int ApplyDamage(Combatant target, int damage)
    {
        var shield = target.StatusEffects.FirstOrDefault(e => e.Tag == SkillEffectTag.Shield);
        if (shield != null)
        {
            var absorbed = Math.Min(damage, (int)shield.Value);
            shield.Value -= absorbed;
            damage -= absorbed;

            if (shield.Value <= 0)
            {
                target.StatusEffects.Remove(shield);
            }
        }

        if (damage > 0)
        {
            target.Character.TakeDamage(damage);
        }

        return damage;
    }

    private float GetElementalMultiplier(Element skillElement, Element targetElement)
    {
        if (skillElement == Element.Earth && targetElement == Element.Water)
        {
            return 1.5f;
        }

        if (skillElement == Element.Water && targetElement == Element.Fire)
        {
            return 1.5f;
        }

        if (skillElement == Element.Fire && targetElement == Element.Wind)
        {
            return 1.5f;
        }

        if (skillElement == Element.Wind && targetElement == Element.Earth)
        {
            return 1.5f;
        }

        if (skillElement == Element.Water && targetElement == Element.Earth)
        {
            return 0.5f;
        }

        if (skillElement == Element.Fire && targetElement == Element.Water)
        {
            return 0.5f;
        }

        if (skillElement == Element.Wind && targetElement == Element.Fire)
        {
            return 0.5f;
        }

        if (skillElement == Element.Earth && targetElement == Element.Wind)
        {
            return 0.5f;
        }

        return 1.0f;
    }

    private float GetControlHitChance(Character attacker, Character defender, float baseChance,
        SkillHitRules? hitRules = null)
    {
        var min = 0.1f;
        var max = 1.0f;

        if (hitRules != null)
        {
            baseChance = hitRules.BaseChance;
            min = hitRules.MinChance;
            max = hitRules.MaxChance;
        }

        // INT vs WIS based chance modification
        var chance = baseChance + (attacker.Int - defender.Wis) * 0.01f;

        // Clamp between min and max
        if (chance < min)
        {
            chance = min;
        }

        if (chance > max)
        {
            chance = max;
        }

        return chance;
    }
}
