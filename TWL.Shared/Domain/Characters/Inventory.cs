using TWL.Shared.Domain.Models;

namespace TWL.Shared.Domain.Characters;

/// <summary>
///     Inventory class that holds multiple ItemSlots.
/// </summary>
public class Inventory
{
    private readonly List<ItemSlot> _slots;

    public Inventory()
    {
        _slots = new List<ItemSlot>();
    }

    public IReadOnlyList<ItemSlot> ItemSlots
    {
        get => _slots.AsReadOnly();
        set
        {
            if (value == null)
            {
                return;
            }

            _slots.Clear();
            foreach (var slot in value)
            {
                if (slot != null)
                {
                    _slots.Add(slot);
                }
            }
        }
    }

    public void AddItem(int itemId, int quantity) => AddItem(itemId, quantity, BindPolicy.Unbound, null);

    public void AddItem(int itemId, int quantity, BindPolicy policy, int? boundToId)
    {
        if (quantity <= 0)
        {
            return;
        }

        // Try to find an existing stack that matches exactly
        var existing = _slots.FirstOrDefault(s => s.ItemId == itemId && s.Policy == policy && s.BoundToId == boundToId);

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _slots.Add(new ItemSlot(itemId, quantity, policy, boundToId));
        }
    }

    public bool HasItem(int itemId, int requiredQuantity)
    {
        if (requiredQuantity <= 0)
        {
            return false;
        }

        var total = _slots.Where(s => s.ItemId == itemId).Sum(s => (long)s.Quantity);
        return total >= requiredQuantity;
    }

    public bool RemoveItem(int itemId, int quantity) => RemoveItem(itemId, quantity, null);

    public bool RemoveItem(int itemId, int quantity, BindPolicy? policyFilter)
    {
        if (quantity <= 0)
        {
            return false;
        }

        var candidates = _slots.Where(s => s.ItemId == itemId).ToList();
        if (policyFilter.HasValue)
        {
            candidates = candidates.Where(s => s.Policy == policyFilter.Value).ToList();
        }

        var totalAvailable = candidates.Sum(s => (long)s.Quantity);
        if (totalAvailable < quantity)
        {
            return false;
        }

        var remaining = quantity;

        foreach (var slot in candidates)
        {
            if (remaining <= 0)
            {
                break;
            }

            var toTake = Math.Min(slot.Quantity, remaining);

            slot.Quantity -= toTake;
            remaining -= toTake;

            if (slot.Quantity <= 0)
            {
                _slots.Remove(slot);
            }
        }

        return true;
    }

    public int GetItemCount(int itemId) => _slots.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
}