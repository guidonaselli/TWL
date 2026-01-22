using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Networking;

/// <summary>
///     Representa un personaje en el lado del servidor.
///     Podrías tener stats completos, estado de combate, etc.
/// </summary>
public class ServerCharacter
{
    private int _hp;

    public int Hp
    {
        get => _hp;
        init => _hp = value;
    }

    public int Id;
    public string Name;

    public int Str;
    // Resto de stats (Con, Int, Spd, etc.)

    private int _exp;
    public int Exp
    {
        get => _exp;
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
            }
        }
    }

    public void AddExp(int amount)
    {
        Interlocked.Add(ref _exp, amount);
    }

    public void AddGold(int amount)
    {
        Interlocked.Add(ref _gold, amount);
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

        return newHp;
    }
}
