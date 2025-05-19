using System.Collections.Generic;
using TWL.Shared.Domain.Models;

namespace TWL.Client.Presentation.Managers;

public static class LootTable
{
    public static List<Item> RollCommonChest()
    {
        return new List<Item> { new Item { ItemId = 1, Name = "Potion", Type = ItemType.Consumable, MaxStack = 10 } };
    }
}