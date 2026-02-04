using System.Reflection;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Security;

public class BindOnEquipHardeningTests
{
    private readonly ServerCharacter _character;
    private readonly TradeManager _tradeManager;

    public BindOnEquipHardeningTests()
    {
        _character = new ServerCharacter
        {
            Id = 100,
            Name = "TestUser"
        };
        _tradeManager = new TradeManager();
    }

    private void AddItemWithType(int itemId, int quantity, ItemType type, BindPolicy policy)
    {
        // Use LoadSaveData to inject items with Type, since AddItem doesn't allow setting Type directly
        var currentData = _character.GetSaveData();

        var newItem = new Item
        {
            ItemId = itemId,
            Quantity = quantity,
            Type = type,
            Policy = policy,
            BoundToId = null
        };

        if (currentData.Inventory == null)
        {
            currentData.Inventory = new List<Item>();
        }
        currentData.Inventory.Add(newItem);

        _character.LoadSaveData(currentData);
    }

    [Fact]
    public void Should_Bind_Item_On_Use()
    {
        // Arrange
        var itemId = 101;
        AddItemWithType(itemId, 1, ItemType.Equipment, BindPolicy.BindOnEquip);

        var initialItem = _character.Inventory.First(i => i.ItemId == itemId);
        Assert.Null(initialItem.BoundToId);
        Assert.Equal(BindPolicy.BindOnEquip, initialItem.Policy);
        Assert.Equal(ItemType.Equipment, initialItem.Type);

        // Act
        // UseItem(0)
        var result = _character.UseItem(0, out var modifiedItem);

        // Assert
        Assert.True(result);
        Assert.NotNull(modifiedItem);
        Assert.Equal(_character.Id, modifiedItem.BoundToId);

        var inventoryItem = _character.Inventory.First(i => i.ItemId == itemId);
        Assert.Equal(_character.Id, inventoryItem.BoundToId);
    }

    [Fact]
    public void Should_Reject_Trade_Of_Used_BindOnEquip_Item()
    {
        // Arrange
        var itemId = 102;
        AddItemWithType(itemId, 1, ItemType.Equipment, BindPolicy.BindOnEquip);

        _character.UseItem(0, out _); // Bind it

        var item = _character.Inventory.First(i => i.ItemId == itemId);
        Assert.Equal(_character.Id, item.BoundToId);

        // Act
        var canTransfer = _tradeManager.ValidateTransfer(item, 200, _character.Id);

        // Assert
        Assert.False(canTransfer, "TradeManager should reject transfer of Bound item");
    }

    [Fact]
    public void Should_Consume_Consumable_On_Use()
    {
        // Arrange
        var itemId = 201;
        AddItemWithType(itemId, 2, ItemType.Consumable, BindPolicy.Unbound);

        // Act
        var result = _character.UseItem(0, out var modifiedItem);

        // Assert
        Assert.True(result);
        Assert.Equal(1, modifiedItem.Quantity);

        var inventoryItem = _character.Inventory.First(i => i.ItemId == itemId);
        Assert.Equal(1, inventoryItem.Quantity);

        // Use again to remove
        var result2 = _character.UseItem(0, out var modifiedItem2);
        Assert.True(result2);
        Assert.Equal(0, modifiedItem2.Quantity);

        Assert.DoesNotContain(_character.Inventory, i => i.ItemId == itemId);
    }
}
