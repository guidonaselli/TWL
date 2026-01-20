using System.Collections.Generic;
using System.Linq;

namespace TWL.Shared.Domain.Characters;

/// <summary>
///     Inventory class that holds multiple ItemSlots.
/// </summary>
public class Inventory
{
    private readonly Dictionary<int, ItemSlot> _slots;

    public Inventory()
    {
        _slots = new Dictionary<int, ItemSlot>();
    }

    public IReadOnlyList<ItemSlot> ItemSlots
    {
        get => _slots.Values.ToList().AsReadOnly();
        set
        {
            if (value == null) return;
            _slots.Clear();
            foreach (var slot in value)
                if (slot != null)
                    _slots[slot.ItemId] = slot;
        }
    }

    public void AddItem(int itemId, int quantity)
    {
        if (quantity <= 0) return;

        if (_slots.TryGetValue(itemId, out var slot))
        {
            slot.Quantity += quantity;
        }
        else
        {
            _slots[itemId] = new ItemSlot(itemId, quantity);
        }
    }

    public bool HasItem(int itemId, int requiredQuantity)
    {
        if (requiredQuantity <= 0) return false;

        return _slots.TryGetValue(itemId, out var slot) && slot.Quantity >= requiredQuantity;
    }

    public bool RemoveItem(int itemId, int quantity)
    {
        if (quantity <= 0) return false;

        if (!_slots.TryGetValue(itemId, out var slot)) return false;
        if (slot.Quantity < quantity) return false;

        slot.Quantity -= quantity;
        if (slot.Quantity <= 0) _slots.Remove(itemId);
        return true;
    }

    public int GetItemCount(int itemId)
    {
        return _slots.TryGetValue(itemId, out var slot) ? slot.Quantity : 0;
    }
}
