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
        foreach(var item in character.Inventory)
        {
            Assert.Equal(100, item.Quantity);
        }
    }
}
