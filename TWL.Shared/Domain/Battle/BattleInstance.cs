using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Shared.Domain.Battle;

public enum BattleState
{
    Active,
    Victory,
    Defeat
}

public class BattleInstance
{
    public List<Combatant> Allies { get; private set; }
    public List<Combatant> Enemies { get; private set; }
    public List<Combatant> AllCombatants => Allies.Concat(Enemies).ToList();

    public BattleState State { get; private set; } = BattleState.Active;

    // Queue of combatants who are ready to act (ATB >= 100)
    private Queue<Combatant> _readyQueue = new();

    // Current combatant whose turn it is
    public Combatant CurrentTurnCombatant { get; private set; }

    // Dependency on Skill Catalog
    private ISkillCatalog _skillCatalog;

    public BattleInstance(IEnumerable<Character> allies, IEnumerable<Character> enemies, ISkillCatalog? skillCatalog = null)
    {
        int idCounter = 1;
        Allies = allies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
        Enemies = enemies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
        _skillCatalog = skillCatalog ?? SkillRegistry.Instance;
    }

    public void Tick(float deltaTimeSeconds)
    {
        if (State != BattleState.Active) return;
        if (CurrentTurnCombatant != null) return; // Waiting for action

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
                        _readyQueue.Enqueue(c);
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
        if (CurrentTurnCombatant == null) return "No active turn";
        if (CurrentTurnCombatant.BattleId != action.ActorId) return "Not your turn";

        var actor = CurrentTurnCombatant;
        var targetCombatant = AllCombatants.FirstOrDefault(c => c.BattleId == action.TargetId);

        string resultMessage = "";

        switch (action.Type)
        {
            case CombatActionType.Attack:
                if (targetCombatant != null)
                {
                    int dmgVal = actor.Character.CalculatePhysicalDamage();
                    if (actor.AttackBuffTurns > 0) dmgVal = (int)(dmgVal * 1.5);

                    int damage = Math.Max(1, dmgVal - targetCombatant.Character.CalculateDefense());
                    if (targetCombatant.IsDefending) damage /= 2;

                    targetCombatant.Character.TakeDamage(damage);
                    resultMessage = $"{actor.Character.Name} attacks {targetCombatant.Character.Name} for {damage} damage!";
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
                if (resultMessage == "Not enough SP!" || resultMessage == "No target") return resultMessage;
                break;

            case CombatActionType.Flee:
                 resultMessage = $"{actor.Character.Name} tries to flee... failed!";
                 break;
        }

        // End turn
        if (actor.AttackBuffTurns > 0) actor.AttackBuffTurns--;

        // Process Status Effects (Burn, etc)
        string statusLogs = ProcessStatusEffects(actor);
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

        if (!actor.Character.ConsumeSp(skill.SpCost)) return "Not enough SP!";

        if (target == null && skill.TargetType == SkillTargetType.SingleEnemy) return "No target";

        // Logic for Data-Driven Skill
        return ApplyDataDrivenSkill(actor, target, skill);
    }

    private string ApplyDataDrivenSkill(Combatant actor, Combatant target, Skill skill)
    {
        // Identify Targets
        List<Combatant> targets = new List<Combatant>();

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
        bool didDamage = false;
        bool didHeal = false;

        // Calculate base power from Scaling
        foreach (var scaling in skill.Scaling)
        {
            float statValue = GetStatValue(actor.Character, scaling.Stat);
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
                     int defense = (skill.Branch == SkillBranch.Magical)
                         ? currentTarget.Character.CalculateMagicalDefense()
                         : currentTarget.Character.CalculateDefense();

                     int damage = Math.Max(1, (int)totalValue - defense);
                     if (currentTarget.IsDefending) damage /= 2;

                     currentTarget.Character.TakeDamage(damage);
                     didDamage = true;
                     // Only track last damage for simple logging
                 }
                 else if (effect.Tag == SkillEffectTag.Heal)
                 {
                     int healAmount = (int)totalValue;
                     if (healAmount == 0) healAmount = (int)effect.Value;

                     currentTarget.Character.Heal(healAmount);
                     didHeal = true;
                 }
                 else if (effect.Tag == SkillEffectTag.Burn || effect.Tag == SkillEffectTag.BuffStats || effect.Tag == SkillEffectTag.DebuffStats)
                 {
                     var rng = new Random();
                     if (rng.NextDouble() <= effect.Chance)
                     {
                         currentTarget.AddStatusEffect(new StatusEffectInstance(effect.Tag, effect.Value, effect.Duration, effect.Param));
                     }
                 }
            }
        }

        if (didDamage) return $"{actor.Character.Name} uses {skill.Name}!";
        if (didHeal) return $"{actor.Character.Name} uses {skill.Name} and heals!";

        return $"{actor.Character.Name} uses {skill.Name}!";
    }

    public string ProcessStatusEffects(Combatant combatant)
    {
        var logs = new List<string>();
        // Process DoTs and Duration
        for (int i = combatant.StatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = combatant.StatusEffects[i];

            if (effect.Tag == SkillEffectTag.Burn)
            {
                int dmg = (int)effect.Value;
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

    private string UseLegacySkill(Combatant actor, Combatant target, int skillId)
    {
        int cost = 0;
        switch (skillId)
        {
            case 1: cost = 5; break;  // Power Strike
            case 2: cost = 10; break; // Fireball
            case 3: cost = 15; break; // Heal
            case 4: cost = 10; break; // Focus (Buff)
        }

        if (!actor.Character.ConsumeSp(cost)) return "Not enough SP!";

        if (target == null && skillId != 3 && skillId != 4) return "No target"; // Heal/Focus might be self

        switch (skillId)
        {
            case 1: // Power Strike (Phys)
                int baseDmg = (int)(actor.Character.CalculatePhysicalDamage() * 1.5);
                if (actor.AttackBuffTurns > 0) baseDmg = (int)(baseDmg * 1.5);

                int dmg1 = Math.Max(1, baseDmg - target.Character.CalculateDefense());
                if (target.IsDefending) dmg1 /= 2;
                target.Character.TakeDamage(dmg1);
                return $"{actor.Character.Name} uses Power Strike on {target.Character.Name} for {dmg1}!";

            case 2: // Fireball (Magic)
                // Magic not affected by physical attack buff
                int dmg2 = Math.Max(1, actor.Character.CalculateMagicalDamage() * 2 - target.Character.CalculateMagicalDefense());
                target.Character.TakeDamage(dmg2);
                return $"{actor.Character.Name} casts Fireball on {target.Character.Name} for {dmg2}!";

            case 3: // Heal
                int heal = actor.Character.Int * 4;
                if (target == null) target = actor;
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

    public void ForceEnd()
    {
        State = BattleState.Defeat;
    }
}
