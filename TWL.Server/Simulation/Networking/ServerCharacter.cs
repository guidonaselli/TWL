using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Networking;

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
    public int Atk => Str * 2;
    public int Def => Con * 2;
    public int Mat => Int * 2;
    public int Mdf => Wis * 2;
    public int Spd => Agi;

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

    private readonly List<int> _pets = new();
    public IReadOnlyList<int> Pets
    {
        get
        {
            lock (_pets)
            {
                return _pets.ToArray();
            }
        }
    }

    public void AddPet(int petId)
    {
        lock (_pets)
        {
            if (!_pets.Contains(petId))
            {
                _pets.Add(petId);
                IsDirty = true;
            }
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
            Gold = _gold
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
            data.Pets = _pets.ToList();
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
                _pets.AddRange(data.Pets);
        }

        IsDirty = false;
    }
}
