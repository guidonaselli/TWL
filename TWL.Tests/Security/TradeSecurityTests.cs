using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using System.Collections.Generic;

namespace TWL.Tests.Security;

public class TradeSecurityTests
{
    private readonly TradeManager _tradeManager;
    private readonly ServerCharacter _alice;
    private readonly ServerCharacter _bob;

    public TradeSecurityTests()
    {
        _tradeManager = new TradeManager();
        _alice = new ServerCharacter { Id = 100, Name = "Alice" };
        _bob = new ServerCharacter { Id = 200, Name = "Bob" };
    }

    [Fact]
    public void Should_Transfer_Unbound_Item()
    {
        // Arrange
        _alice.AddItem(1, 10, BindPolicy.Unbound);

        // Act
        var result = _tradeManager.TransferItem(_alice, _bob, 1, 5);

        // Assert
        Assert.True(result);
        Assert.True(_alice.HasItem(1, 5));
        Assert.True(_bob.HasItem(1, 5));

        // Check Bob's item policy
        var bobItems = _bob.GetItems(1);
        Assert.Single(bobItems);
        Assert.Equal(BindPolicy.Unbound, bobItems[0].Policy);
    }

    [Fact]
    public void Should_Reject_Bound_Item()
    {
        // Arrange
        _alice.AddItem(2, 10, BindPolicy.CharacterBound, _alice.Id);

        // Act
        var result = _tradeManager.TransferItem(_alice, _bob, 2, 5);

        // Assert
        Assert.False(result);
        Assert.True(_alice.HasItem(2, 10)); // No change
        Assert.False(_bob.HasItem(2, 1));
    }

    [Fact]
    public void Should_Reject_AccountBound_To_Different_User()
    {
        // Arrange
        _alice.AddItem(3, 10, BindPolicy.AccountBound, _alice.Id);

        // Act
        var result = _tradeManager.TransferItem(_alice, _bob, 3, 5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Should_Reject_Insufficient_Quantity()
    {
        // Arrange
        _alice.AddItem(4, 2, BindPolicy.Unbound);

        // Act
        var result = _tradeManager.TransferItem(_alice, _bob, 4, 5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Should_Reject_Mixed_Stack_If_Insufficient_Tradable()
    {
        // Arrange
        _alice.AddItem(5, 5, BindPolicy.Unbound);
        _alice.AddItem(5, 5, BindPolicy.CharacterBound); // Same ItemId, diff policy

        // Act - Try to trade 8. Should only have 5 tradable.
        var result = _tradeManager.TransferItem(_alice, _bob, 5, 8);

        // Assert
        Assert.False(result);
        Assert.True(_alice.HasItem(5, 10)); // Total 10
    }

    [Fact]
    public void Should_Transfer_Only_Tradable_From_Mixed_Stack()
    {
        // Arrange
        _alice.AddItem(6, 10, BindPolicy.Unbound);
        _alice.AddItem(6, 5, BindPolicy.CharacterBound);

        // Act - Trade 8 (within tradable limit)
        var result = _tradeManager.TransferItem(_alice, _bob, 6, 8);

        // Assert
        Assert.True(result);

        // Verify Alice has 2 Unbound left + 5 Bound left
        var unbound = _alice.GetItems(6, BindPolicy.Unbound);
        var bound = _alice.GetItems(6, BindPolicy.CharacterBound);

        Assert.Single(unbound);
        Assert.Equal(2, unbound[0].Quantity);

        Assert.Single(bound);
        Assert.Equal(5, bound[0].Quantity);

        // Verify Bob got 8 Unbound
        var bobItems = _bob.GetItems(6);
        Assert.Single(bobItems);
        Assert.Equal(8, bobItems[0].Quantity);
        Assert.Equal(BindPolicy.Unbound, bobItems[0].Policy);
    }
}
