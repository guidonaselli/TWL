﻿namespace TWL.Shared.Domain.Characters;

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
            if (value == null) return;
            _slots.Clear();
            foreach (var slot in value)
                if (slot != null)
                    _slots.Add(slot);
        }
    }

    public void AddItem(int itemId, int quantity)
    {
        if (quantity <= 0) return;
        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        if (slot != null)
            slot.Quantity += quantity;
        else
            _slots.Add(new ItemSlot(itemId, quantity));
    }

    public bool HasItem(int itemId, int requiredQuantity)
    {
        if (requiredQuantity <= 0) return false;
        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        return slot != null && slot.Quantity >= requiredQuantity;
    }

    public bool RemoveItem(int itemId, int quantity)
    {
        if (quantity <= 0) return false;
        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        if (slot == null) return false;
        if (slot.Quantity < quantity) return false;

        slot.Quantity -= quantity;
        if (slot.Quantity <= 0) _slots.Remove(slot);
        return true;
    }

    public int GetItemCount(int itemId)
    {
        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        return slot != null ? slot.Quantity : 0;
    }
}