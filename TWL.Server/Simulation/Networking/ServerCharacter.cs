using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Networking;

public class SkillMastery
{
    public int Rank { get; set; } = 1;
    public int UsageCount { get; set; } = 0;
}

/// <summary>
///     Representa un personaje en el lado del servidor.
///     Podrías tener stats completos, estado de combate, etc.
/// </summary>
public class ServerCharacter
{
    public bool IsDirty { get; set; }

    private int _hp;
    private int _sp;

    public int Hp
    {
        get => _hp;
        init => _hp = value;
    }

    public int Sp
    {
        get => _sp;
        init => _sp = value;
    }

    public int Id;
    public string Name;
    public TWL.Shared.Domain.Characters.Element CharacterElement { get; set; }

    public List<int> KnownSkills { get; set; } = new();

    // Stats & Progression
    public int Level { get; private set; } = 1;
    public int ExpToNextLevel { get; private set; } = 100;
    public int StatPoints { get; private set; } = 0;

    public int Str { get; set; } = 8;
    public int Con { get; set; } = 8;
    public int Int { get; set; } = 8;
    public int Wis { get; set; } = 8;
    public int Agi { get; set; } = 8;

    // Derived Battle Stats
    public int Atk => (Str * 2) + GetStatModifier("Atk");
    public int Def => (Con * 2) + GetStatModifier("Def");
    public int Mat => (Int * 2) + GetStatModifier("Mat");
    public int Mdf => (Wis * 2) + GetStatModifier("Mdf");
    public int Spd => Agi + GetStatModifier("Spd");

    public string ActivePetInstanceId { get; private set; }

    private int GetStatModifier(string stat)
    {
        int modifier = 0;
        lock (_statusLock)
        {
            foreach (var effect in _statusEffects)
            {
                if (string.Equals(effect.Param, stat, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (effect.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.BuffStats)
                        modifier += (int)effect.Value;
                    else if (effect.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.DebuffStats)
                        modifier -= (int)effect.Value;
                }
            }
        }
        return modifier;
    }

    public int MaxHealth => Con * 10;
    public int MaxSp => Int * 5;

    private int _exp;
    private readonly object _progressLock = new();

    public int Exp
    {
        get
        {
            lock (_progressLock) return _exp;
        }
        init => _exp = value;
    }

    private int _gold;
    public int Gold
    {
        get => _gold;
        init => _gold = value;
    }

    private long _premiumCurrency;
    public long PremiumCurrency
    {
        get => Interlocked.Read(ref _premiumCurrency);
        init => _premiumCurrency = value;
    }

    private readonly List<Item> _inventory = new();
    public IReadOnlyList<Item> Inventory
    {
        get
        {
            lock (_inventory)
            {
                // Return deep copies to prevent external modification without lock
                return _inventory.Select(i => new Item
                {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Type = i.Type,
                    MaxStack = i.MaxStack,
                    Quantity = i.Quantity,
                    ForgeSuccessRateBonus = i.ForgeSuccessRateBonus
                }).ToArray();
            }
        }
    }

    public ConcurrentDictionary<int, SkillMastery> SkillMastery { get; private set; } = new();

    // Combat Status Effects
    private readonly List<TWL.Shared.Domain.Battle.StatusEffectInstance> _statusEffects = new();
    private readonly object _statusLock = new();

    public IReadOnlyList<TWL.Shared.Domain.Battle.StatusEffectInstance> StatusEffects
    {
        get
        {
            lock (_statusLock)
            {
                return _statusEffects.ToArray();
            }
        }
    }

    public void AddStatusEffect(TWL.Shared.Domain.Battle.StatusEffectInstance effect, TWL.Shared.Services.IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.Apply(_statusEffects, effect);
            IsDirty = true;
        }
    }

    public float GetResistance(string resistanceTag)
    {
        // Return resistance value from stats/buffs
        // E.g. "SealResist" might be a stat modifier
        return GetStatModifier(resistanceTag);
    }

    public void RemoveStatusEffect(TWL.Shared.Domain.Battle.StatusEffectInstance effect)
    {
        lock (_statusLock)
        {
            _statusEffects.Remove(effect);
            IsDirty = true;
        }
    }

    public void CleanseDebuffs(TWL.Shared.Services.IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.RemoveAll(_statusEffects, e => e.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.DebuffStats ||
                                                  e.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.Burn ||
                                                  e.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.Seal);
            IsDirty = true;
        }
    }

    public void DispelBuffs(TWL.Shared.Services.IStatusEngine engine)
    {
        lock (_statusLock)
        {
            engine.RemoveAll(_statusEffects, e => e.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.BuffStats ||
                                                  e.Tag == TWL.Shared.Domain.Skills.SkillEffectTag.Shield);
            IsDirty = true;
        }
    }

    public int IncrementSkillUsage(int skillId)
    {
        var mastery = SkillMastery.GetOrAdd(skillId, _ => new SkillMastery());
        mastery.UsageCount++;

        // Default rank up logic: every 10 uses
        if (mastery.UsageCount % 10 == 0)
        {
            mastery.Rank++;
        }
        return mastery.Rank;
    }

    public void ReplaceSkill(int oldId, int newId)
    {
        lock (KnownSkills)
        {
            if (KnownSkills.Contains(oldId))
            {
                KnownSkills.Remove(oldId);
                if (!KnownSkills.Contains(newId))
                {
                    KnownSkills.Add(newId);
                }
                IsDirty = true;
            }
        }
    }

    public bool LearnSkill(int skillId)
    {
        lock (KnownSkills)
        {
            if (KnownSkills.Contains(skillId))
            {
                return false;
            }
            KnownSkills.Add(skillId);
            IsDirty = true;
            return true;
        }
    }

    private readonly List<ServerPet> _pets = new();
    public IReadOnlyList<ServerPet> Pets
    {
        get
        {
            lock (_pets)
            {
                return _pets.ToArray();
            }
        }
    }

    public void AddPet(ServerPet pet)
    {
        lock (_pets)
        {
            _pets.Add(pet);
            IsDirty = true;
        }
    }

    public bool SetActivePet(string instanceId)
    {
        lock (_pets)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                ActivePetInstanceId = null;
                IsDirty = true;
                return true;
            }

            var pet = _pets.Find(p => p.InstanceId == instanceId);
            if (pet == null) return false; // Pet not owned

            ActivePetInstanceId = instanceId;
            IsDirty = true;
            return true;
        }
    }

    public ServerPet? GetActivePet()
    {
        lock (_pets)
        {
            if (string.IsNullOrEmpty(ActivePetInstanceId)) return null;
            return _pets.Find(p => p.InstanceId == ActivePetInstanceId);
        }
    }

    public void AddExp(int amount)
    {
        lock (_progressLock)
        {
            _exp += amount;
            while (_exp >= ExpToNextLevel)
            {
                _exp -= ExpToNextLevel;
                Level++;
                ExpToNextLevel = (int)(ExpToNextLevel * 1.2);
                StatPoints += 3;
            }
            IsDirty = true;
        }
    }

    public void AddGold(int amount)
    {
        Interlocked.Add(ref _gold, amount);
        IsDirty = true;
    }

    public void AddPremiumCurrency(long amount)
    {
        Interlocked.Add(ref _premiumCurrency, amount);
        IsDirty = true;
    }

    public bool TryConsumePremiumCurrency(long amount)
    {
        if (amount < 0) return false;
        long initial, current;
        do
        {
            initial = Interlocked.Read(ref _premiumCurrency);
            if (initial < amount) return false;
            current = initial - amount;
        }
        while (Interlocked.CompareExchange(ref _premiumCurrency, current, initial) != initial);

        IsDirty = true;
        return true;
    }

    public bool ConsumeSp(int amount)
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

    public void AddItem(int itemId, int quantity)
    {
        lock (_inventory)
        {
            var existing = _inventory.Find(i => i.ItemId == itemId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _inventory.Add(new Item { ItemId = itemId, Quantity = quantity });
            }
            IsDirty = true;
        }
    }

    public bool HasItem(int itemId, int quantity)
    {
        lock (_inventory)
        {
            var item = _inventory.Find(i => i.ItemId == itemId);
            return item != null && item.Quantity >= quantity;
        }
    }

    public bool RemoveItem(int itemId, int quantity)
    {
        lock (_inventory)
        {
            var item = _inventory.Find(i => i.ItemId == itemId);
            if (item == null || item.Quantity < quantity)
            {
                return false;
            }

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _inventory.Remove(item);
            }
            IsDirty = true;
            return true;
        }
    }

    /// <summary>
    /// Applies damage to the character in a thread-safe manner.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    /// <returns>The new HP value.</returns>
    public int ApplyDamage(int damage)
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

    public int Heal(int amount)
    {
        if (amount <= 0) return _hp;
        int initialHp, newHp;
        do
        {
            initialHp = _hp;
            newHp = initialHp + amount;
            if (newHp > MaxHealth) newHp = MaxHealth;
        }
        while (Interlocked.CompareExchange(ref _hp, newHp, initialHp) != initialHp);

        IsDirty = true;
        return newHp;
    }

    public ServerCharacterData GetSaveData()
    {
        var data = new ServerCharacterData
        {
            Id = Id,
            Name = Name,
            Hp = _hp,
            Sp = _sp,
            Str = Str,
            Con = Con,
            Int = Int,
            Wis = Wis,
            Agi = Agi,
            Gold = _gold,
            PremiumCurrency = _premiumCurrency,
            ActivePetInstanceId = ActivePetInstanceId
        };

        lock (_progressLock)
        {
            data.Exp = _exp;
            data.Level = Level;
            data.ExpToNextLevel = ExpToNextLevel;
            data.StatPoints = StatPoints;
        }

        lock (_inventory)
        {
            data.Inventory = _inventory.Select(i => new Item
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Type = i.Type,
                MaxStack = i.MaxStack,
                Quantity = i.Quantity,
                ForgeSuccessRateBonus = i.ForgeSuccessRateBonus
            }).ToList();
        }

        lock (_pets)
        {
            data.Pets = _pets.Select(p => p.GetSaveData()).ToList();
        }

        return data;
    }

    public void LoadSaveData(ServerCharacterData data)
    {
        Id = data.Id;
        Name = data.Name;
        _hp = data.Hp;
        _sp = data.Sp;
        Str = data.Str;
        Con = data.Con;
        Int = data.Int;
        Wis = data.Wis;
        Agi = data.Agi;
        _gold = data.Gold;
        _premiumCurrency = data.PremiumCurrency;
        ActivePetInstanceId = data.ActivePetInstanceId;

        lock (_progressLock)
        {
            _exp = data.Exp;
            Level = data.Level;
            ExpToNextLevel = data.ExpToNextLevel;
            StatPoints = data.StatPoints;
        }

        lock (_inventory)
        {
            _inventory.Clear();
            if (data.Inventory != null)
                _inventory.AddRange(data.Inventory);
        }

        lock (_pets)
        {
            _pets.Clear();
            if (data.Pets != null)
            {
                foreach (var petData in data.Pets)
                {
                    var pet = new ServerPet();
                    pet.LoadSaveData(petData);
                    _pets.Add(pet);
                }
            }
        }

        IsDirty = false;
    }
}
