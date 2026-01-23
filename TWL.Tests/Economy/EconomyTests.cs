using System.Collections.Concurrent;
using System.Threading.Tasks;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyTests
{
    [Fact]
    public void PurchaseFlow_Success_And_Idempotency()
    {
        var manager = new EconomyManager();
        var charId = 1;
        var character = new ServerCharacter { Id = charId, PremiumCurrency = 0 };

        // 1. Initiate
        var intent = manager.InitiatePurchase(charId, "gems_100"); // 100 Gems
        Assert.NotNull(intent);
        Assert.NotNull(intent.OrderId);

        // 2. Verify (First time)
        var result1 = manager.VerifyPurchase(charId, intent.OrderId, "valid_token", character);
        Assert.True(result1.Success);
        Assert.Equal(100, character.PremiumCurrency);
        Assert.Equal(100, result1.NewBalance);

        // 3. Verify (Second time - Idempotency)
        var result2 = manager.VerifyPurchase(charId, intent.OrderId, "valid_token", character);
        Assert.True(result2.Success);
        Assert.Equal("Already completed", result2.Message);
        Assert.Equal(100, character.PremiumCurrency); // Should still be 100
    }

    [Fact]
    public void VerifyPurchase_Fails_UserMismatch()
    {
        var manager = new EconomyManager();
        var charId1 = 1;
        var charId2 = 2;
        var character = new ServerCharacter { Id = charId1 };

        var intent = manager.InitiatePurchase(charId1, "gems_100");

        // Try verifying with wrong user ID
        var result = manager.VerifyPurchase(charId2, intent.OrderId, "token", character);
        Assert.False(result.Success);
        Assert.Equal("User mismatch", result.Message);
    }

    [Fact]
    public void BuyShopItem_Success()
    {
        var manager = new EconomyManager();
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 20 }; // Start with 20

        // Shop Item 1: Cost 10, ItemId 101
        var result = manager.BuyShopItem(character, 1, 1);

        Assert.True(result.Success);
        Assert.Equal(10, character.PremiumCurrency);
        Assert.True(character.HasItem(101, 1));
    }

    [Fact]
    public void BuyShopItem_InsufficientFunds()
    {
        var manager = new EconomyManager();
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 5 }; // Only 5

        // Shop Item 1: Cost 10
        var result = manager.BuyShopItem(character, 1, 1);

        Assert.False(result.Success);
        Assert.Equal("Insufficient funds", result.Message);
        Assert.Equal(5, character.PremiumCurrency);
        Assert.False(character.HasItem(101, 1));
    }

    [Fact]
    public async Task BuyShopItem_Concurrency_NoDoubleSpend()
    {
        var manager = new EconomyManager();
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 15 }; // Enough for 1 item (Cost 10), but not 2

        // Try to buy 2 items in parallel
        var task1 = Task.Run(() => manager.BuyShopItem(character, 1, 1));
        var task2 = Task.Run(() => manager.BuyShopItem(character, 1, 1));

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result, task2.Result };
        int successes = 0;
        foreach (var r in results) if (r.Success) successes++;

        Assert.Equal(1, successes); // Only one should succeed
        Assert.Equal(5, character.PremiumCurrency); // 15 - 10 = 5
        Assert.True(character.HasItem(101, 1));
    }
}
