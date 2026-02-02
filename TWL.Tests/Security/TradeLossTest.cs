using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Security;

public class TradeLossTest
{
    private readonly ServerCharacter _alice;
    private readonly ServerCharacter _bob;
    private readonly TradeManager _tradeManager;

    public TradeLossTest()
    {
        _tradeManager = new TradeManager();
        _alice = new ServerCharacter { Id = 100, Name = "Alice" };
        _bob = new ServerCharacter { Id = 200, Name = "Bob" };
    }

    [Fact]
    public void Should_Not_Lose_Item_When_Target_Inventory_Full()
    {
        // Arrange
        _alice.AddItem(1001, 1);
        _bob.MaxInventorySlots = 1;
        _bob.AddItem(2002, 1); // Fill Bob's inventory with something else

        // Act
        // Attempt to transfer item 1001 from Alice to Bob.
        // Bob is full (1/1 slots used).
        var result = _tradeManager.TransferItem(_alice, _bob, 1001, 1);

        // Assert
        Assert.False(result, "Transfer should fail if target is full");
        Assert.True(_alice.HasItem(1001, 1), "Alice should still have the item (No Loss)");
        Assert.False(_bob.HasItem(1001, 1), "Bob should not have the item");
    }
}
