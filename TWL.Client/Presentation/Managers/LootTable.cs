using TWL.Client.Presentation.Services;
using TWL.Shared.Domain.Models;

namespace TWL.Client.Presentation.Managers;

public static class LootTable
{
    public static List<Item> RollCommonChest() => new()
        { new Item { ItemId = 1, Name = Loc.T("ITEM_Potion"), Type = ItemType.Consumable, MaxStack = 10 } };
}