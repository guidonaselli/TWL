using System;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyHardeningTests : IDisposable
{
    private string _tempLedger;
    private EconomyManager _economy;
    private ServerCharacter _character;

    public EconomyHardeningTests()
    {
        _tempLedger = Path.GetTempFileName();
        _economy = new EconomyManager(_tempLedger);
        _character = new ServerCharacter
        {
            Id = 1,
            Name = "TestUser",
            MaxInventorySlots = 10,
            PremiumCurrency = 1000 // Start with 1000 gems
        };
    }

    public void Dispose()
    {
        _economy?.Dispose();
        if (File.Exists(_tempLedger))
        {
            try { File.Delete(_tempLedger); } catch { }
        }
    }

    [Fact]
    public void RateLimit_ShouldBlockExcessiveRequests()
    {
        // Limit is 10 per minute by default
        for (int i = 0; i < 10; i++)
        {
            var result = _economy.InitiatePurchase(_character.Id, "gems_100");
            Assert.NotNull(result);
        }

        // 11th should fail
        var blocked = _economy.InitiatePurchase(_character.Id, "gems_100");
        Assert.Null(blocked);
    }

    [Fact]
    public void Idempotency_BuyShopItem_ShouldPreventDoubleSpend()
    {
        // Shop Item 1 costs 10 Gems.
        var opId = "unique-op-1";

        var res1 = _economy.BuyShopItem(_character, 1, 1, opId);
        Assert.True(res1.Success);
        Assert.Equal(990, _character.PremiumCurrency);

        // Retry with same opId
        var res2 = _economy.BuyShopItem(_character, 1, 1, opId);
        Assert.True(res2.Success); // Should return success (Idempotent)
        Assert.Equal("Already completed", res2.Message);
        Assert.Equal(990, _character.PremiumCurrency); // Balance should NOT decrease again
    }

    [Fact]
    public void Compensation_ShouldRefund_WhenInventoryFull()
    {
        _character.MaxInventorySlots = 0; // Cannot add any NEW item stacks

        var initialBalance = _character.PremiumCurrency;

        // Try to buy Item 1 (Potion)
        var res = _economy.BuyShopItem(_character, 1, 1, "op-fail-inventory");

        Assert.False(res.Success);
        Assert.Equal("Inventory full", res.Message);
        Assert.Equal(initialBalance, _character.PremiumCurrency);

        // Check Ledger for failure log
        _economy.Dispose(); // Flush logs
        var log = File.ReadAllText(_tempLedger);
        Assert.Contains("ShopBuyFailed", log);
        Assert.Contains("Reason:InventoryFull", log);
    }

    [Fact]
    public void ValidateReceipt_FailClosed_OnEmptyInput()
    {
        var intent = _economy.InitiatePurchase(_character.Id, "gems_100"); // uses 1 rate limit token
        Assert.NotNull(intent);

        // Try verify with empty receipt
        var res = _economy.VerifyPurchase(_character.Id, intent.OrderId, "", _character);
        Assert.False(res.Success);

        // Try verify with valid receipt (mock)
        var res2 = _economy.VerifyPurchase(_character.Id, intent.OrderId, $"mock_sig_{intent.OrderId}", _character);
        Assert.True(res2.Success);
    }
}
