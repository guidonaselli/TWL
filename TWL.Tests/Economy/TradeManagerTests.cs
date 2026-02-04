using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using TWL.Server.Security;

namespace TWL.Tests.Economy;

public class TradeManagerTests
{
    private readonly TradeManager _tradeManager;
    private readonly ServerCharacter _source;
    private readonly ServerCharacter _target;

    public TradeManagerTests()
    {
        _tradeManager = new TradeManager();
        _source = new ServerCharacter
        {
            Id = 100,
            Name = "Source",
            MaxInventorySlots = 10
        };
        _target = new ServerCharacter
        {
            Id = 200,
            Name = "Target",
            MaxInventorySlots = 10
        };
    }

    [Fact]
    public void ValidateTransfer_Unbound_ShouldAllow()
    {
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.Unbound };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.True(result);
    }

    [Fact]
    public void ValidateTransfer_BindOnEquip_ShouldAllow_WhenNotBound()
    {
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.BindOnEquip, BoundToId = null };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.True(result);
    }

    [Fact]
    public void ValidateTransfer_BindOnEquip_ShouldReject_WhenBound()
    {
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.BindOnEquip, BoundToId = _source.Id };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.False(result);
    }

    [Fact]
    public void ValidateTransfer_BindOnPickup_ShouldReject()
    {
        // BoP implies it's already bound if it's in inventory (usually), but ValidateTransfer checks policy too.
        // Even if BoundToId is null (which shouldn't happen for BoP in inventory), logic rejects BoP.
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.BindOnPickup };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.False(result);
    }

    [Fact]
    public void ValidateTransfer_CharacterBound_ShouldReject()
    {
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.CharacterBound };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.False(result);
    }

    [Fact]
    public void ValidateTransfer_AccountBound_ShouldReject_DifferentCharacters()
    {
        // Assuming different IDs mean different characters (and thus trade rejected)
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.AccountBound, BoundToId = _source.Id };
        var result = _tradeManager.ValidateTransfer(item, _target.Id, _source.Id);
        Assert.False(result);
    }

    [Fact]
    public void ValidateTransfer_AccountBound_ShouldAllow_SameCharacter_Technically()
    {
        // ValidateTransfer returns true if target == source for AccountBound,
        // effectively only allowing transfer to self (which TransferItem blocks separately)
        var item = new Item { ItemId = 1, Quantity = 1, Policy = BindPolicy.AccountBound, BoundToId = _source.Id };
        var result = _tradeManager.ValidateTransfer(item, _source.Id, _source.Id);
        Assert.True(result);
    }

    [Fact]
    public void TransferItem_Unbound_ShouldSucceed()
    {
        // Arrange
        _source.AddItem(1, 5, BindPolicy.Unbound);

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 2);

        // Assert
        Assert.True(result);
        Assert.Equal(3, _source.GetItems(1).Sum(i => i.Quantity));
        Assert.Equal(2, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_BindOnPickup_ShouldFail()
    {
        // Arrange
        _source.AddItem(1, 5, BindPolicy.BindOnPickup); // Will auto-bind to Source

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 2);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _source.GetItems(1).Sum(i => i.Quantity));
        Assert.Equal(0, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_BindOnEquip_ShouldSucceed_WhenNotBound()
    {
        // Arrange
        // AddItem with BoE doesn't auto-bind (boundToId stays null)
        _source.AddItem(1, 1, BindPolicy.BindOnEquip);

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 1);

        // Assert
        Assert.True(result);
        Assert.Equal(0, _source.GetItems(1).Sum(i => i.Quantity));
        Assert.Equal(1, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_InsufficientQuantity_ShouldFail()
    {
        // Arrange
        _source.AddItem(1, 1, BindPolicy.Unbound);

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 5);

        // Assert
        Assert.False(result);
        Assert.Equal(1, _source.GetItems(1).Sum(i => i.Quantity));
        Assert.Equal(0, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_TargetInventoryFull_ShouldRollback()
    {
        // Arrange
        _source.AddItem(1, 5, BindPolicy.Unbound);
        _target.MaxInventorySlots = 0; // Force full

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 2);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _source.GetItems(1).Sum(i => i.Quantity)); // Rolled back
        Assert.Equal(0, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_SelfTransfer_ShouldFail()
    {
        // Arrange
        _source.AddItem(1, 5, BindPolicy.Unbound);

        // Act
        var result = _tradeManager.TransferItem(_source, _source, 1, 2);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _source.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_MixedPolicies_ShouldOnlyTransferTradable()
    {
        // Arrange
        _source.AddItem(1, 2, BindPolicy.Unbound);
        _source.AddItem(1, 2, BindPolicy.BindOnPickup); // Bound, untradable

        // Check setup
        Assert.Equal(4, _source.GetItems(1).Sum(i => i.Quantity));

        // Act: Try to transfer 3. Only 2 are tradable.
        var result = _tradeManager.TransferItem(_source, _target, 1, 3);

        // Assert
        Assert.False(result); // Should fail because requested 3 but only 2 available
        Assert.Equal(4, _source.GetItems(1).Sum(i => i.Quantity)); // No change
        Assert.Equal(0, _target.GetItems(1).Sum(i => i.Quantity));
    }

    [Fact]
    public void TransferItem_ShouldNotTransferBoundOnEquip_IfBound()
    {
        // Arrange
        _source.AddItem(1, 1, BindPolicy.BindOnEquip, _source.Id); // Explicitly bound

        // Act
        var result = _tradeManager.TransferItem(_source, _target, 1, 1);

        // Assert
        Assert.False(result);
        Assert.Equal(1, _source.GetItems(1).Sum(i => i.Quantity));
    }
}
