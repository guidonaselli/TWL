using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

namespace TWL.Tests.Security;

public class TradeBindHardeningTests
{
    private readonly ServerCharacter _alice;
    private readonly ServerCharacter _bob;
    private readonly TradeManager _tradeManager;

    public TradeBindHardeningTests()
    {
        _tradeManager = new TradeManager();
        _alice = new ServerCharacter { Id = 100, Name = "Alice" };
        _bob = new ServerCharacter { Id = 200, Name = "Bob" };
    }

    [Fact]
    public void Should_Reject_Trade_Of_Bound_BindOnEquip_Item()
    {
        // Arrange: Alice has a BindOnEquip sword.
        // It IS bound to her (simulating she equipped it).
        _alice.AddItem(101, 1, BindPolicy.BindOnEquip, _alice.Id);

        // Verify setup
        var items = _alice.GetItems(101);
        Assert.Single(items);
        Assert.Equal(BindPolicy.BindOnEquip, items[0].Policy);
        Assert.Equal(_alice.Id, items[0].BoundToId);

        // Act: Try to trade it to Bob
        // Current Vulnerability: TradeManager sees "BindOnEquip" and returns TRUE, ignoring BoundToId.
        var result = _tradeManager.TransferItem(_alice, _bob, 101, 1);

        // Assert: MUST FAIL
        Assert.False(result, "Security Fail: Allowed trading a bound BindOnEquip item!");

        // Double check Bob didn't get it
        Assert.False(_bob.HasItem(101, 1));
        Assert.True(_alice.HasItem(101, 1));
    }

    [Fact]
    public void Gifting_BindOnPickup_Should_Set_BoundToId()
    {
        // Arrange: Economy Manager
        var tempLedger = Path.GetTempFileName();
        var economy = new EconomyManager(tempLedger);

        try
        {
            // Setup: ShopItem 3 is BindOnPickup (from EconomyManager mock data)
            // { 3, (100, 103, BindPolicy.BindOnPickup) }

            _alice.AddPremiumCurrency(1000); // Giver

            // Act: Alice gifts Item 3 to Bob
            var result = economy.GiftShopItem(_alice, _bob, 3, 1, "gift-op-1");

            // Assert
            Assert.True(result.Success);

            // Verify Bob received it
            var bobItems = _bob.GetItems(103);
            Assert.Single(bobItems);

            var item = bobItems[0];
            Assert.Equal(BindPolicy.BindOnPickup, item.Policy);

            // CRITICAL CHECK: It must be bound to Bob
            // Current Vulnerability: It is likely null
            Assert.NotNull(item.BoundToId);
            Assert.Equal(_bob.Id, item.BoundToId);
        }
        finally
        {
            economy.Dispose();
            if (File.Exists(tempLedger))
            {
                File.Delete(tempLedger);
            }
        }
    }
}