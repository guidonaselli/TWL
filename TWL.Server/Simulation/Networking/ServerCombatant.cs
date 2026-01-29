using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Networking;

public class SkillMastery
{
    public int Rank { get; set; } = 1;
    public int UsageCount { get; set; } = 0;
}

public abstract class ServerCombatant
{
    public int Id;
    public string Name;
    public Element CharacterElement { get; set; }

    // Stats
    protected int _hp;
    protected int _sp;

    public virtual int Hp
    {
        get => _hp;
        set => _hp = value;
    }

    public virtual int Sp
    {
        get => _sp;
        set => _sp = value;
    }

    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Agi { get; set; }

    public virtual int MaxHp => Con * 10; // Default logic, override in Pet
    public virtual int MaxSp => Int * 5;  // Default logic, override in Pet

    // Derived Battle Stats
    public virtual int Atk => (Str * 2) + GetStatModifier("Atk");
    public virtual int Def => (Con * 2) + GetStatModifier("Def");
    public virtual int Mat => (Int * 2) + GetStatModifier("Mat");
    public virtual int Mdf => (Wis * 2) + GetStatModifier("Mdf");
    public virtual int Spd => Agi + GetStatModifier("Spd");

    public int? LastAttackerId { get; set; }

    public bool IsDirty { get; set; } // Simplified for Combatant, Character will override

    // Skills
    public ConcurrentDictionary<int, SkillMastery> SkillMastery { get; protected set; } = new();

    // Cooldowns (Transient)
    private readonly ConcurrentDictionary<int, int> _activeCooldowns = new();

    // Status Effects
    protected readonly List<StatusEffectInstance> _statusEffects = new();
    protected readonly object _statusLock = new();

    public IReadOnlyList<StatusEffectInstance> StatusEffects
    {
        get
        {
            lock (_statusLock)
            {
                return _statusEffects.ToArray();
            }
        }
    }

    protected int GetStatModifier(string stat)
    {
        int modifier = 0;
        lock (_statusLock)
        {
            foreach (var effect in _statusEffects)
            {
                if (string.Equals(effect.Param, stat, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (effect.Tag == SkillEffectTag.BuffStats)
                        modifier += (int)effect.Value;
                    else if (effect.Tag == SkillEffectTag.DebuffStats)
                        modifier -= (int)effect.Value;
                }
            }
        }
        return modifier;
    }

    public void AddStatusEffect(StatusEffectInstance effect, IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.Apply(_statusEffects, effect);
            IsDirty = true;
        }
    }

    public float GetResistance(string resistanceTag)
    {
        float modifier = 0f;
        lock (_statusLock)
        {
            foreach (var effect in _statusEffects)
            {
                if (string.Equals(effect.Param, resistanceTag, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (effect.Tag == SkillEffectTag.BuffStats)
                        modifier += effect.Value;
                    else if (effect.Tag == SkillEffectTag.DebuffStats)
                        modifier -= effect.Value;
                }
            }
        }
        return modifier;
    }

    public void RemoveStatusEffect(StatusEffectInstance effect)
    {
        lock (_statusLock)
        {
            _statusEffects.Remove(effect);
            IsDirty = true;
        }
    }

    public void CleanseDebuffs(IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.RemoveAll(_statusEffects, e => e.Tag == SkillEffectTag.DebuffStats ||
                                                  e.Tag == SkillEffectTag.Burn ||
                                                  e.Tag == SkillEffectTag.Seal);
            IsDirty = true;
        }
    }

    public void DispelBuffs(IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.RemoveAll(_statusEffects, e => e.Tag == SkillEffectTag.BuffStats ||
                                                  e.Tag == SkillEffectTag.Shield);
            IsDirty = true;
        }
    }

    public virtual int IncrementSkillUsage(int skillId)
    {
        var mastery = SkillMastery.GetOrAdd(skillId, _ => new SkillMastery());
        mastery.UsageCount++;

        if (mastery.UsageCount % 10 == 0)
        {
            mastery.Rank++;
        }
        IsDirty = true;
        return mastery.Rank;
    }

    public bool IsSkillOnCooldown(int skillId)
    {
        return _activeCooldowns.TryGetValue(skillId, out int turns) && turns > 0;
    }

    public void SetSkillCooldown(int skillId, int turns)
    {
        if (turns > 0)
        {
            _activeCooldowns[skillId] = turns;
        }
    }

    public void TickCooldowns()
    {
        foreach (var kvp in _activeCooldowns)
        {
            if (kvp.Value > 0)
            {
                int newValue = kvp.Value - 1;
                if (newValue <= 0)
                {
                    _activeCooldowns.TryRemove(kvp.Key, out _);
                }
                else
                {
                    _activeCooldowns[kvp.Key] = newValue;
                }
            }
        }
    }

    public abstract void ReplaceSkill(int oldId, int newId);

    public virtual int ApplyDamage(int damage)
    {
        int initialHp, newHp;
        do
        {
            initialHp = _hp;
            newHp = initialHp - damage;
            if (newHp < 0) newHp = 0;
        }
        while (Interlocked.CompareExchange(ref _hp, newHp, initialHp) != initialHp);

        IsDirty = true;
        return newHp;
    }

    public virtual int Heal(int amount)
    {
        if (amount <= 0) return _hp;
        int initialHp, newHp;
        do
        {
            initialHp = _hp;
            newHp = initialHp + amount;
            if (newHp > MaxHp) newHp = MaxHp;
        }
        while (Interlocked.CompareExchange(ref _hp, newHp, initialHp) != initialHp);

        IsDirty = true;
        return newHp;
    }

    public virtual bool ConsumeSp(int amount)
    {
        int initialSp, newSp;
        do
        {
            initialSp = _sp;
            if (initialSp < amount) return false;
            newSp = initialSp - amount;
        }
        while (Interlocked.CompareExchange(ref _sp, newSp, initialSp) != initialSp);

        IsDirty = true;
        return true;
    }
}
