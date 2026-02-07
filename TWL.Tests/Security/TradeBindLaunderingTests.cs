using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;
using System.Linq;

namespace TWL.Tests.Security;

public class TradeBindLaunderingTests
{
    private readonly ServerCharacter _alice;
    private readonly ServerCharacter _bob;
    private readonly TradeManager _tradeManager;

    public TradeBindLaunderingTests()
    {
        _tradeManager = new TradeManager();
        _alice = new ServerCharacter { Id = 100, Name = "Alice" };
        _bob = new ServerCharacter { Id = 200, Name = "Bob" };
    }

    [Fact]
    public void Should_Not_Launder_Bound_Items_When_Mixed_With_Unbound()
    {
        int itemId = 101;

        // Arrange: Alice has 1 Bound Sword and 1 Unbound Sword.
        // Order matters for the naive RemoveItem bug.
        // If we add Bound first, it might be at index 0.

        // 1. Add Bound Item
        _alice.AddItem(itemId, 1, BindPolicy.BindOnEquip, _alice.Id);

        // 2. Add Unbound Item
        _alice.AddItem(itemId, 1, BindPolicy.BindOnEquip, null);

        // Verify setup
        var items = _alice.GetItems(itemId);
        Assert.Equal(2, items.Sum(i => i.Quantity));
        Assert.Contains(items, i => i.BoundToId == _alice.Id);
        Assert.Contains(items, i => i.BoundToId == null);

        // Act: Alice trades 1 Sword to Bob.
        // The TradeManager should calculate she has 1 tradable sword.
        bool result = _tradeManager.TransferItem(_alice, _bob, itemId, 1);

        // Assert
        Assert.True(result, "Trade should succeed because Alice has 1 unbound sword.");

        // Check Bob
        var bobItems = _bob.GetItems(itemId);
        Assert.Single(bobItems);
        Assert.Equal(BindPolicy.BindOnEquip, bobItems[0].Policy);
        Assert.Null(bobItems[0].BoundToId); // Bob should get Unbound (or Bound to him if BindOnPickup, but this is BindOnEquip)

        // Check Alice - CRITICAL
        // Alice should still have the BOUND sword.
        var aliceRemaining = _alice.GetItems(itemId);
        Assert.Single(aliceRemaining);
        Assert.Equal(1, aliceRemaining[0].Quantity);

        // If the bug exists, she might have the Unbound sword left (Bound one was taken)
        Assert.NotNull(aliceRemaining[0].BoundToId);
        Assert.Equal(_alice.Id, aliceRemaining[0].BoundToId);
    }
}
