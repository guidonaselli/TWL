using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

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
    public void Inventory_ShouldReturnCachedCopy_And_IsolateFromInternalState()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 10);

        // 1. First Access (Creates Cache)
        var items1 = character.Inventory;
        Assert.Single(items1);
        Assert.Equal(10, items1[0].Quantity);

        // 2. Second Access (Should Return Same Cache for Performance)
        var items2 = character.Inventory;
        Assert.Same(items1, items2);

        // 3. Modify Cache (Should not affect Internal State, but will affect subsequent reads of Cache)
        items1[0].Quantity = 999;

        // Since we are caching the reference, modifying the object inside the list
        // is visible to anyone else holding the reference or getting the property.
        // This is the trade-off for performance.
        Assert.Equal(999, character.Inventory[0].Quantity);

        // 4. Verify Internal State is Protected (by invalidating cache/adding item)
        // If we trigger a change, the cache should be rebuilt from Internal State.
        character.AddItem(102, 1); // Triggers Invalidation

        var items3 = character.Inventory;
        Assert.NotSame(items1, items3); // New object

        // The modification to the OLD cache (999) should NOT have affected the Internal State.
        // So the item with ID 101 should still be 10.
        var originalItem = items3.First(i => i.ItemId == 101);
        Assert.Equal(10, originalItem.Quantity);
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

        var result = character.RemoveItem(101, 3);
        Assert.True(result);
        Assert.Equal(2, character.Inventory.First(i => i.ItemId == 101).Quantity);
    }

    [Fact]
    public void RemoveItem_ShouldReturnFalseAndNotChange_WhenNotEnoughQuantity()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);

        var result = character.RemoveItem(101, 6);
        Assert.False(result);
        Assert.Equal(5, character.Inventory.First(i => i.ItemId == 101).Quantity);
    }

    [Fact]
    public void HasItem_ShouldWorkWithMultipleStacksAndPolicies()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddItem(101, 5);
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