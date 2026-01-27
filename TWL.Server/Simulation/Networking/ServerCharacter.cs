using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Networking;

/// <summary>
///     Representa un personaje en el lado del servidor.
/// </summary>
public class ServerCharacter : ServerCombatant
{
    // Override IsDirty to include pets
    private bool _isDirty;
    public new bool IsDirty
    {
        get
        {
            if (_isDirty || base.IsDirty) return true;
            lock (_pets)
            {
                return _pets.Any(p => p.IsDirty);
            }
        }
        set
        {
            _isDirty = value;
            base.IsDirty = value;
            if (!value)
            {
                lock (_pets)
                {
                    foreach (var p in _pets) p.IsDirty = false;
                }
            }
        }
    }

    // Stats & Progression
    public List<int> KnownSkills { get; set; } = new();
    public int Level { get; private set; } = 1;
    public int ExpToNextLevel { get; private set; } = 100;
    public int StatPoints { get; private set; } = 0;

    public string ActivePetInstanceId { get; private set; }

    // Legacy/Alias support if needed, but preferable to use MaxHp from base
    public int MaxHealth => MaxHp;

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

    public int MaxInventorySlots { get; set; } = 100;

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
                    ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                    Policy = i.Policy,
                    BoundToId = i.BoundToId
                }).ToArray();
            }
        }
    }

    public override void ReplaceSkill(int oldId, int newId)
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

    public bool RemovePet(string instanceId)
    {
        lock (_pets)
        {
            var pet = _pets.Find(p => p.InstanceId == instanceId);
            if (pet == null) return false;

            _pets.Remove(pet);

            if (ActivePetInstanceId == instanceId)
            {
                ActivePetInstanceId = null;
            }

            IsDirty = true;
            return true;
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

    // ConsumeSp is inherited from ServerCombatant

    public bool AddItem(int itemId, int quantity, BindPolicy policy = BindPolicy.Unbound, int? boundToId = null)
    {
        lock (_inventory)
        {
            // Only stack if ID, Policy and BoundToId match
            var existing = _inventory.Find(i => i.ItemId == itemId && i.Policy == policy && i.BoundToId == boundToId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                IsDirty = true;
                return true;
            }
            else
            {
                if (_inventory.Count >= MaxInventorySlots) return false;
                _inventory.Add(new Item { ItemId = itemId, Quantity = quantity, Policy = policy, BoundToId = boundToId });
                IsDirty = true;
                return true;
            }
        }
    }

    public bool HasItem(int itemId, int quantity)
    {
        lock (_inventory)
        {
            long total = 0;
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId)
                {
                    total += item.Quantity;
                }
            }
            return total >= quantity;
        }
    }

    public bool RemoveItem(int itemId, int quantity)
    {
        return RemoveItem(itemId, quantity, null);
    }

    public bool RemoveItem(int itemId, int quantity, BindPolicy? policyFilter)
    {
        lock (_inventory)
        {
            // Calculate total available first to ensure atomicity
            long total = 0;
            var candidates = new List<Item>();
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId)
                {
                    if (policyFilter.HasValue && item.Policy != policyFilter.Value) continue;
                    total += item.Quantity;
                    candidates.Add(item);
                }
            }

            if (total < quantity) return false;

            // Remove from candidates
            int remainingToRemove = quantity;
            foreach (var item in candidates)
            {
                if (remainingToRemove <= 0) break;

                int toTake = System.Math.Min(item.Quantity, remainingToRemove);
                item.Quantity -= toTake;
                remainingToRemove -= toTake;

                if (item.Quantity <= 0)
                {
                    _inventory.Remove(item);
                }
            }
            IsDirty = true;
            return true;
        }
    }

    public List<Item> GetItems(int itemId, BindPolicy? policyFilter = null)
    {
        lock (_inventory)
        {
            var results = new List<Item>();
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId)
                {
                    if (policyFilter.HasValue && item.Policy != policyFilter.Value) continue;
                    // Return copies
                    results.Add(new Item
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Type = item.Type,
                        MaxStack = item.MaxStack,
                        Quantity = item.Quantity,
                        Policy = item.Policy,
                        BoundToId = item.BoundToId
                    });
                }
            }
            return results;
        }
    }

    // ApplyDamage, Heal are inherited

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
                ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                Policy = i.Policy,
                BoundToId = i.BoundToId
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
