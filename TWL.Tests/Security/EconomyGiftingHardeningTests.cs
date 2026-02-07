using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

namespace TWL.Tests.Security;

public class EconomyGiftingHardeningTests
{
    private const int ITEM_ID_COST_100 = 3; // From EconomyManager mock data: ShopItem 3 cost 100
    private const long DAILY_LIMIT = 5000;

    [Fact]
    public void Gift_WithinLimit_Succeeds_And_Increments_Accumulator()
    {
        using var manager = new EconomyManager(Path.GetTempFileName());
        var giver = new ServerCharacter { Id = 1, PremiumCurrency = 10000 };
        var receiver = new ServerCharacter { Id = 2 };

        // Gift 10 units (1000 Gems cost)
        var result = manager.GiftShopItem(giver, receiver, ITEM_ID_COST_100, 10, "OP-1");

        Assert.True(result.Success);
        Assert.Equal(9000, giver.PremiumCurrency);
        Assert.Equal(1000, giver.DailyGiftAccumulator); // 10 * 100
        Assert.Equal(DateTime.UtcNow.Date, giver.LastGiftResetDate);
    }

    [Fact]
    public void Gift_ExceedsLimit_Fails_And_Preserves_Currency()
    {
        using var manager = new EconomyManager(Path.GetTempFileName());
        var giver = new ServerCharacter { Id = 1, PremiumCurrency = 10000 };
        var receiver = new ServerCharacter { Id = 2 };

        // Attempt to gift 51 units (5100 Gems cost) > 5000 Limit
        var result = manager.GiftShopItem(giver, receiver, ITEM_ID_COST_100, 51, "OP-FAIL");

        Assert.False(result.Success);
        Assert.Equal("Daily gift limit exceeded", result.Message);
        Assert.Equal(10000, giver.PremiumCurrency); // No funds taken
        Assert.Equal(0, giver.DailyGiftAccumulator);
    }

    [Fact]
    public void Gift_Partial_Accumulation_Until_Limit()
    {
        using var manager = new EconomyManager(Path.GetTempFileName());
        var giver = new ServerCharacter { Id = 1, PremiumCurrency = 10000 };
        var receiver = new ServerCharacter { Id = 2 };

        // 1. Gift 40 units (4000 cost) - OK
        var result1 = manager.GiftShopItem(giver, receiver, ITEM_ID_COST_100, 40, "OP-PART-1");
        Assert.True(result1.Success);
        Assert.Equal(4000, giver.DailyGiftAccumulator);

        // 2. Gift 10 units (1000 cost) - 4000 + 1000 = 5000 - OK (Exact Limit)
        var result2 = manager.GiftShopItem(giver, receiver, ITEM_ID_COST_100, 10, "OP-PART-2");
        Assert.True(result2.Success);
        Assert.Equal(5000, giver.DailyGiftAccumulator);

        // 3. Gift 1 unit (100 cost) - 5100 > 5000 - FAIL
        var result3 = manager.GiftShopItem(giver, receiver, ITEM_ID_COST_100, 1, "OP-PART-3");
        Assert.False(result3.Success);
        Assert.Equal("Daily gift limit exceeded", result3.Message);

        // Funds consumed only for first two
        Assert.Equal(5000, giver.PremiumCurrency);
    }

    [Fact]
    public void Persistence_Saves_And_Loads_Accumulator()
    {
        var giver = new ServerCharacter { Id = 1, PremiumCurrency = 10000 };

        // Manually manipulate via public property/method logic simulation
        // Since we can't easily mock time inside ServerCharacter without refactoring,
        // we test the Save/Load logic directly.

        // Simulate accumulation by calling TryConsume
        giver.TryConsumeDailyGiftLimit(1234, 5000);

        var savedData = giver.GetSaveData();
        Assert.Equal(1234, savedData.DailyGiftAccumulator);
        Assert.Equal(DateTime.UtcNow.Date, savedData.LastGiftResetDate);

        // Load into new character
        var loadedChar = new ServerCharacter();
        loadedChar.LoadSaveData(savedData);

        Assert.Equal(1234, loadedChar.DailyGiftAccumulator);
        Assert.Equal(DateTime.UtcNow.Date, loadedChar.LastGiftResetDate);
    }
}
