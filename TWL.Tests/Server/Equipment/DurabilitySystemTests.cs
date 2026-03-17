using System;
using System.Collections.Generic;
using TWL.Server.Services.Combat;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Server.Equipment;

public class DurabilitySystemTests
{
    private readonly DeathPenaltyService _deathPenaltyService;

    public DurabilitySystemTests()
    {
        _deathPenaltyService = new DeathPenaltyService();
    }

    [Fact]
    public void Item_IsBroken_TrueWhenDurabilityZeroAndMaxDurabilityGreaterThanZero()
    {
        // Arrange & Act
        var item1 = new Item { Durability = 0, MaxDurability = 100 };
        var item2 = new Item { Durability = -1, MaxDurability = 100 };
        var item3 = new Item { Durability = 10, MaxDurability = 100 };
        var item4 = new Item { Durability = 0, MaxDurability = 0 };

        // Assert
        Assert.True(item1.IsBroken);
        Assert.True(item2.IsBroken);
        Assert.False(item3.IsBroken);
        Assert.False(item4.IsBroken); // Items with 0 MaxDurability are indestructible
    }

    [Fact]
    public void DeathPenalty_ReducesEquippedItemsDurabilityByOne()
    {
        // Arrange
        var character = new ServerCharacter();
        var item1 = new Item { ItemId = 1, MaxDurability = 100, Durability = 50 };
        var item2 = new Item { ItemId = 2, MaxDurability = 100, Durability = 1 };
        var item3 = new Item { ItemId = 3, MaxDurability = 0, Durability = 0 }; // Indestructible

        // Equip items directly to bypass inventory binding logic
        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData
        {
            Equipment = new List<Item> { item1, item2, item3 }
        });

        // Act
        _deathPenaltyService.ApplyExpPenalty(character, Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(49, character.Equipment[0].Durability);
        Assert.Equal(0, character.Equipment[1].Durability);
        Assert.Equal(0, character.Equipment[2].Durability); // Should not decrease below 0 or change

        Assert.False(character.Equipment[0].IsBroken);
        Assert.True(character.Equipment[1].IsBroken);
        Assert.False(character.Equipment[2].IsBroken); // Because MaxDurability is 0
    }

    [Fact]
    public void BrokenItems_DoNotContributeToStats()
    {
        // Arrange
        var character = new ServerCharacter();
        character.ResetStatsToBaseline(); // Ensure baseline stats (Str 8)

        // Force reload character with baseline stats
        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData { Str = 8 });

        int baseAtk = character.Atk; // This is the base Atk (Str * 2 = 8 * 2 = 16)

        var sword = new Item
        {
            ItemId = 1,
            MaxDurability = 100,
            Durability = 100,
            EnhancementStats = new Dictionary<string, float> { { "Atk", 50 } }
        };

        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData
        {
            Str = 8,
            Equipment = new List<Item> { sword }
        });

        // Act & Assert 1: Stat should be added
        Assert.Equal(16 + 50, character.Atk);

        // Act & Assert 2: Break the item, stat should be removed (need to load again as Equipment cache makes them immutable)
        sword.Durability = 0;
        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData
        {
            Str = 8,
            Equipment = new List<Item> { sword }
        });

        Assert.True(character.Equipment[0].IsBroken);
        Assert.Equal(16, character.Atk); // Stat is no longer applied
    }

    [Fact]
    public void LegacySaves_MissingDurability_HandledAsIndestructible()
    {
        // Arrange
        var characterData = new TWL.Server.Persistence.ServerCharacterData
        {
            Equipment = new List<Item>
            {
                new Item { ItemId = 1 } // No Durability or MaxDurability set (simulate legacy deserialization)
            }
        };

        var character = new ServerCharacter();

        // Act
        character.LoadSaveData(characterData);

        // Assert
        Assert.Single(character.Equipment);
        Assert.Equal(0, character.Equipment[0].MaxDurability);
        Assert.Equal(0, character.Equipment[0].Durability);
        Assert.False(character.Equipment[0].IsBroken);
    }
}
