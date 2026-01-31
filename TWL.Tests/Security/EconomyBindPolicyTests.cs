using System.Linq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Security;

public class EconomyBindPolicyTests
{
    [Fact]
    public void SharedInventory_Separates_Bound_And_Unbound_Items()
    {
        var inventory = new Inventory();

        // Add 10 Unbound Swords (ID 100)
        inventory.AddItem(100, 10, BindPolicy.Unbound, null);

        // Add 5 Bound Swords (ID 100)
        inventory.AddItem(100, 5, BindPolicy.BindOnPickup, null);

        // Should have 2 slots
        Assert.Equal(2, inventory.ItemSlots.Count);

        var unboundSlot = inventory.ItemSlots.FirstOrDefault(s => s.Policy == BindPolicy.Unbound);
        var boundSlot = inventory.ItemSlots.FirstOrDefault(s => s.Policy == BindPolicy.BindOnPickup);

        Assert.NotNull(unboundSlot);
        Assert.Equal(10, unboundSlot.Quantity);

        Assert.NotNull(boundSlot);
        Assert.Equal(5, boundSlot.Quantity);

        // GetItemCount should return total
        Assert.Equal(15, inventory.GetItemCount(100));
    }

    [Fact]
    public void SharedInventory_Add_Merges_Correct_Stack()
    {
        var inventory = new Inventory();
        inventory.AddItem(100, 10, BindPolicy.Unbound, null);
        inventory.AddItem(100, 5, BindPolicy.Unbound, null); // Should merge

        Assert.Single(inventory.ItemSlots);
        Assert.Equal(15, inventory.ItemSlots[0].Quantity);
    }

    [Fact]
    public void EconomyManager_ShopBuy_Grants_Bound_Item_When_Configured()
    {
        using var manager = new EconomyManager(System.IO.Path.GetTempFileName());
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 1000 };

        // Shop Item 3 is configured as BindOnPickup in EconomyManager (hardcoded mock data)
        // { 3, (100, 103, BindPolicy.BindOnPickup) }

        var result = manager.BuyShopItem(character, 3, 1);

        Assert.True(result.Success);

        // Verify Character Inventory has the item with correct policy
        var items = character.GetItems(103);
        Assert.Single(items);
        Assert.Equal(BindPolicy.BindOnPickup, items[0].Policy);
    }

    [Fact]
    public void EconomyManager_ShopBuy_Grants_Unbound_Item_When_Configured()
    {
        using var manager = new EconomyManager(System.IO.Path.GetTempFileName());
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 1000 };

        // Shop Item 1 is configured as Unbound
        // { 1, (10, 101, BindPolicy.Unbound) }

        var result = manager.BuyShopItem(character, 1, 1);

        Assert.True(result.Success);

        var items = character.GetItems(101);
        Assert.Single(items);
        Assert.Equal(BindPolicy.Unbound, items[0].Policy);
    }

    [Fact]
    public void TradeManager_Respects_BindPolicy()
    {
        var tradeManager = new TradeManager();
        var source = new ServerCharacter { Id = 1 };
        var target = new ServerCharacter { Id = 2 };

        // Add Bound Item to Source
        source.AddItem(200, 1, BindPolicy.BindOnPickup);

        // Attempt transfer
        bool success = tradeManager.TransferItem(source, target, 200, 1);

        Assert.False(success); // Should fail because it's bound
        Assert.False(target.HasItem(200, 1));
        Assert.True(source.HasItem(200, 1));
    }

    [Fact]
    public void TradeManager_Allows_Unbound_Transfer()
    {
        var tradeManager = new TradeManager();
        var source = new ServerCharacter { Id = 1 };
        var target = new ServerCharacter { Id = 2 };

        // Add Unbound Item to Source
        source.AddItem(200, 1, BindPolicy.Unbound);

        // Attempt transfer
        bool success = tradeManager.TransferItem(source, target, 200, 1);

        Assert.True(success);
        Assert.True(target.HasItem(200, 1));
        Assert.False(source.HasItem(200, 1));
    }

    [Fact]
    public void EconomyManager_Idempotency_Prevent_Double_Spend()
    {
        using var manager = new EconomyManager(System.IO.Path.GetTempFileName());
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 100 }; // 100 Gems

        // Shop Item 1 costs 10 Gems.
        // Buy with Operation ID "OP-1"
        var result1 = manager.BuyShopItem(character, 1, 1, "OP-1");

        Assert.True(result1.Success);
        Assert.Equal(90, character.PremiumCurrency);

        // Replay with same Operation ID
        var result2 = manager.BuyShopItem(character, 1, 1, "OP-1");

        Assert.True(result2.Success); // Idempotent success
        Assert.Equal("Already completed", result2.Message);
        Assert.Equal(90, character.PremiumCurrency); // Should NOT deduct again
    }
}
