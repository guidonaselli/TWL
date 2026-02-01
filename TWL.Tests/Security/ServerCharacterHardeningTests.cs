using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

namespace TWL.Tests.Security;

public class ServerCharacterHardeningTests
{
    [Fact]
    public void AddItem_BindOnPickup_ShouldAutomaticallyBindToOwner()
    {
        var character = new ServerCharacter { Id = 123 };

        // Act: Add BoP item without specifying boundToId
        character.AddItem(1, 1, BindPolicy.BindOnPickup);

        // Assert
        var items = character.GetItems(1);
        Assert.Single(items);
        Assert.Equal(BindPolicy.BindOnPickup, items[0].Policy);
        Assert.Equal(123, items[0].BoundToId);
    }

    [Fact]
    public void AddItem_CharacterBound_ShouldAutomaticallyBindToOwner()
    {
        var character = new ServerCharacter { Id = 456 };

        // Act
        character.AddItem(2, 1, BindPolicy.CharacterBound);

        // Assert
        var items = character.GetItems(2);
        Assert.Single(items);
        Assert.Equal(BindPolicy.CharacterBound, items[0].Policy);
        Assert.Equal(456, items[0].BoundToId);
    }

    [Fact]
    public void AddItem_Unbound_ShouldNotBind()
    {
        var character = new ServerCharacter { Id = 789 };

        // Act
        character.AddItem(3, 1);

        // Assert
        var items = character.GetItems(3);
        Assert.Single(items);
        Assert.Equal(BindPolicy.Unbound, items[0].Policy);
        Assert.Null(items[0].BoundToId);
    }

    [Fact]
    public void AddItem_BindOnEquip_ShouldNotBindInitially()
    {
        var character = new ServerCharacter { Id = 101 };

        // Act
        character.AddItem(4, 1, BindPolicy.BindOnEquip);

        // Assert
        var items = character.GetItems(4);
        Assert.Single(items);
        Assert.Equal(BindPolicy.BindOnEquip, items[0].Policy);
        Assert.Null(items[0].BoundToId);
    }

    [Fact]
    public void AddItem_BindOnPickup_WithExplicitBind_ShouldRespectExplicit()
    {
        var character = new ServerCharacter { Id = 999 };

        // Act: Bind to someone else (e.g. gifting scenario where policy allows it before pickup?
        // Or maybe just testing the method signature)
        // The method signature allows it.
        character.AddItem(5, 1, BindPolicy.BindOnPickup, 888);

        // Assert
        var items = character.GetItems(5);
        Assert.Single(items);
        Assert.Equal(888, items[0].BoundToId);
    }
}