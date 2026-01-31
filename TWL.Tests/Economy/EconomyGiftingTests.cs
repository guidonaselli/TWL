using System;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyGiftingTests : IDisposable
{
    private string _tempLedger;
    private EconomyManager _economy;
    private ServerCharacter _giver;
    private ServerCharacter _receiver;

    public EconomyGiftingTests()
    {
        _tempLedger = Path.GetTempFileName();
        _economy = new EconomyManager(_tempLedger);

        _giver = new ServerCharacter
        {
            Id = 100,
            Name = "Giver",
            PremiumCurrency = 1000,
            MaxInventorySlots = 10
        };

        _receiver = new ServerCharacter
        {
            Id = 200,
            Name = "Receiver",
            PremiumCurrency = 0,
            MaxInventorySlots = 10
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
    public void GiftShopItem_Success_DebitsGiver_CreditsReceiver()
    {
        // Shop Item 1: Cost 10, Unbound
        var result = _economy.GiftShopItem(_giver, _receiver, 1, 1, "gift-op-1");

        Assert.True(result.Success);
        Assert.Equal(990, _giver.PremiumCurrency); // 1000 - 10
        Assert.Equal(0, _receiver.PremiumCurrency); // Receiver pays nothing

        // Receiver has item
        Assert.True(_receiver.HasItem(101, 1));

        // Ledger Check
        _economy.Dispose(); // Flush logs
        var log = File.ReadAllText(_tempLedger);
        Assert.Contains("GiftBuy", log);
        Assert.Contains("To:200", log);
    }

    [Fact]
    public void GiftShopItem_InsufficientFunds_Fails()
    {
        _giver = new ServerCharacter
        {
            Id = 100,
            Name = "Giver",
            PremiumCurrency = 5, // Not enough for Item 1 (Cost 10)
            MaxInventorySlots = 10
        };

        var result = _economy.GiftShopItem(_giver, _receiver, 1, 1, "gift-op-fail");

        Assert.False(result.Success);
        Assert.Equal("Insufficient funds", result.Message);
        Assert.Equal(5, _giver.PremiumCurrency);
        Assert.False(_receiver.HasItem(101, 1));
    }

    [Fact]
    public void GiftShopItem_ReceiverInventoryFull_RefundsGiver()
    {
        _receiver.MaxInventorySlots = 0; // Full

        var result = _economy.GiftShopItem(_giver, _receiver, 1, 1, "gift-op-full");

        Assert.False(result.Success);
        Assert.Equal("Receiver inventory full", result.Message);

        // Verify Refund (Compensation)
        Assert.Equal(1000, _giver.PremiumCurrency);

        // Ledger Check
        _economy.Dispose(); // Flush logs
        var log = File.ReadAllText(_tempLedger);
        Assert.Contains("GiftBuyFailed", log);
        Assert.Contains("Reason:ReceiverInventoryFull", log);
    }

    [Fact]
    public void GiftShopItem_Idempotency_PreventsDoubleSpend()
    {
        var opId = "gift-op-idempotent";

        // First call
        var res1 = _economy.GiftShopItem(_giver, _receiver, 1, 1, opId);
        Assert.True(res1.Success);
        Assert.Equal(990, _giver.PremiumCurrency);

        // Second call
        var res2 = _economy.GiftShopItem(_giver, _receiver, 1, 1, opId);
        Assert.True(res2.Success);
        Assert.Equal("Already completed", res2.Message);

        // Balance should remain same as after first call
        Assert.Equal(990, _giver.PremiumCurrency);

        // Receiver should have only 1 item (logic depends on how many times AddItem was called,
        // but Manager prevents second call to AddItem)
        Assert.True(_receiver.HasItem(101, 1));
        Assert.False(_receiver.HasItem(101, 2));
    }

    [Fact]
    public void GiftShopItem_AppliesBindPolicyToReceiver()
    {
        // Shop Item 3: Cost 100, BindOnPickup (Item 103)
        var result = _economy.GiftShopItem(_giver, _receiver, 3, 1, "gift-op-bind");

        Assert.True(result.Success);

        // Receiver should have Item 103 with BindOnPickup
        var items = _receiver.GetItems(103);
        Assert.Single(items);
        Assert.Equal(BindPolicy.BindOnPickup, items[0].Policy);
    }
}
