using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Shared;

public class InventoryTests
{
    [Fact]
    public void AddItem_AddNewItem_ShouldIncreaseCount()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        Assert.Equal(10, inventory.GetItemCount(1));
    }

    [Fact]
    public void AddItem_AddExistingItem_ShouldIncreaseQuantity()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        inventory.AddItem(1, 5);
        Assert.Equal(15, inventory.GetItemCount(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_AddZeroOrNegativeQuantity_ShouldNotChangeInventory(int quantity)
    {
        var inventory = new Inventory();
        inventory.AddItem(1, quantity);
        Assert.Equal(0, inventory.GetItemCount(1));
    }

    [Fact]
    public void HasItem_ItemExistsWithSufficientQuantity_ShouldReturnTrue()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        Assert.True(inventory.HasItem(1, 10));
    }

    [Fact]
    public void HasItem_ItemExistsWithInsufficientQuantity_ShouldReturnFalse()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 5);
        Assert.False(inventory.HasItem(1, 10));
    }

    [Fact]
    public void HasItem_ItemDoesNotExist_ShouldReturnFalse()
    {
        var inventory = new Inventory();
        Assert.False(inventory.HasItem(1, 1));
    }

    [Fact]
    public void RemoveItem_RemovePartialQuantity_ShouldDecreaseQuantity()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        inventory.RemoveItem(1, 5);
        Assert.Equal(5, inventory.GetItemCount(1));
    }

    [Fact]
    public void RemoveItem_RemoveFullQuantity_ShouldRemoveItem()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        inventory.RemoveItem(1, 10);
        Assert.Equal(0, inventory.GetItemCount(1));
        Assert.False(inventory.HasItem(1, 1));
    }

    [Fact]
    public void RemoveItem_RemoveMoreThanAvailable_ShouldReturnFalse()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 5);
        Assert.False(inventory.RemoveItem(1, 10));
        Assert.Equal(5, inventory.GetItemCount(1));
    }

    [Fact]
    public void RemoveItem_RemoveNonExistingItem_ShouldReturnFalse()
    {
        var inventory = new Inventory();
        Assert.False(inventory.RemoveItem(1, 1));
    }

    [Fact]
    public void GetItemCount_ItemExists_ShouldReturnQuantity()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);
        Assert.Equal(10, inventory.GetItemCount(1));
    }

    [Fact]
    public void GetItemCount_ItemDoesNotExist_ShouldReturnZero()
    {
        var inventory = new Inventory();
        Assert.Equal(0, inventory.GetItemCount(1));
    }

    [Fact]
    public void ItemSlots_Setter_ShouldReplaceAllItems()
    {
        var inventory = new Inventory();
        inventory.AddItem(1, 10);

        var newItemSlots = new[]
        {
            new ItemSlot(2, 20),
            new ItemSlot(3, 30)
        };

        inventory.ItemSlots = newItemSlots;

        Assert.Equal(0, inventory.GetItemCount(1));
        Assert.Equal(20, inventory.GetItemCount(2));
        Assert.Equal(30, inventory.GetItemCount(3));
        Assert.Equal(2, inventory.ItemSlots.Count);
    }
}