using System.Linq;
using System.Threading.Tasks;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests;

public class ServerCharacterTests
{
    [Fact]
    public void AddGold_ShouldBeThreadSafe()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        Parallel.For(0, 1000, _ => character.AddGold(1));
        Assert.Equal(1000, character.Gold);
    }

    [Fact]
    public void AddItem_ShouldUpdateQuantity_WhenItemExists()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 1);
        character.AddItem(101, 2);

        Assert.Single(character.Inventory);
        Assert.Equal(101, character.Inventory[0].ItemId);
        Assert.Equal(3, character.Inventory[0].Quantity);
    }

    [Fact]
    public void AddItem_ShouldAddNewItem_WhenItemDoesNotExist()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 1);
        character.AddItem(102, 1);

        Assert.Equal(2, character.Inventory.Count);
    }

    [Fact]
    public void Inventory_ShouldReturnCopies()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 10);

        var items = character.Inventory;
        items[0].Quantity = 999; // Modify the copy

        // Original should remain unchanged
        Assert.Equal(10, character.Inventory[0].Quantity);
    }

    [Fact]
    public void AddItem_ShouldBeThreadSafe()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        Parallel.For(0, 1000, i =>
        {
            character.AddItem(i % 10, 1); // 10 unique items, adding 100 each
        });

        Assert.Equal(10, character.Inventory.Count);
        foreach (var item in character.Inventory)
        {
            Assert.Equal(100, item.Quantity);
        }
    }

    [Fact]
    public void HasItem_ShouldReturnTrue_WhenEnoughQuantity()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);
        Assert.True(character.HasItem(101, 3));
        Assert.True(character.HasItem(101, 5));
    }

    [Fact]
    public void HasItem_ShouldReturnFalse_WhenNotEnoughQuantity()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);
        Assert.False(character.HasItem(101, 6));
        Assert.False(character.HasItem(102, 1));
    }

    [Fact]
    public void RemoveItem_ShouldRemoveAndReturnTrue_WhenEnoughQuantity()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);

        bool result = character.RemoveItem(101, 3);
        Assert.True(result);
        Assert.Equal(2, character.Inventory.First(i => i.ItemId == 101).Quantity);
    }

    [Fact]
    public void RemoveItem_ShouldReturnFalseAndNotChange_WhenNotEnoughQuantity()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);

        bool result = character.RemoveItem(101, 6);
        Assert.False(result);
        Assert.Equal(5, character.Inventory.First(i => i.ItemId == 101).Quantity);
    }

    [Fact]
    public void HasItem_ShouldWorkWithMultipleStacksAndPolicies()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5, BindPolicy.Unbound);
        character.AddItem(101, 5, BindPolicy.CharacterBound); // Should be a separate stack/item in list

        // Ensure we have 2 items
        // Note: AddItem logic:
        // var existing = _inventory.Find(i => i.ItemId == itemId && i.Policy == policy && i.BoundToId == boundToId);
        // The second AddItem has different policy, so it creates a new item.

        Assert.Equal(2, character.Inventory.Count);
        Assert.True(character.HasItem(101, 10)); // Total 10

        character.RemoveItem(101, 6); // Removes 6 total

        Assert.True(character.HasItem(101, 4));
        Assert.False(character.HasItem(101, 5));
    }
}
