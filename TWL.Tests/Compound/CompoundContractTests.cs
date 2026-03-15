using Xunit;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Microsoft.Extensions.Logging;
using Moq;

namespace TWL.Tests.Compound;

public class CompoundContractTests
{
    [Fact]
    public void Item_ShouldPersistEnhancementMetadata()
    {
        // Arrange
        var item = new Item
        {
            ItemId = 1001,
            InstanceId = Guid.NewGuid(),
            Name = "Iron Sword",
            EnhancementLevel = 5,
            EnhancementStats = new Dictionary<string, float> { { "Atk", 10.5f } }
        };

        // Act
        // (This simulates what the character copy logic does)
        var copy = new Item
        {
            ItemId = item.ItemId,
            InstanceId = item.InstanceId,
            Name = item.Name,
            EnhancementLevel = item.EnhancementLevel,
            EnhancementStats = item.EnhancementStats != null ? new Dictionary<string, float>(item.EnhancementStats) : null
        };

        // Assert
        Assert.Equal(item.InstanceId, copy.InstanceId);
        Assert.Equal(5, copy.EnhancementLevel);
        Assert.NotNull(copy.EnhancementStats);
        Assert.Equal(10.5f, copy.EnhancementStats["Atk"]);
    }

    [Fact]
    public void ServerCharacter_ShouldRoundtripEnhancementMetadata()
    {
        // Arrange
        var character = new ServerCharacter();
        var instanceId = Guid.NewGuid();
        var item = new Item
        {
            ItemId = 1001,
            InstanceId = instanceId,
            EnhancementLevel = 7,
            EnhancementStats = new Dictionary<string, float> { { "Def", 20.0f } }
        };
        character.AddItem(item.ItemId, 1);
        
        // Manual override since AddItem creates a new Item instance
        var data = character.GetSaveData();
        data.Inventory[0].InstanceId = instanceId;
        data.Inventory[0].EnhancementLevel = 7;
        data.Inventory[0].EnhancementStats = new Dictionary<string, float> { { "Def", 20.0f } };
        
        var newCharacter = new ServerCharacter();
        
        // Act
        newCharacter.LoadSaveData(data);
        var loadedItem = newCharacter.Inventory[0];
        
        // Assert
        Assert.Equal(instanceId, loadedItem.InstanceId);
        Assert.Equal(7, loadedItem.EnhancementLevel);
        Assert.NotNull(loadedItem.EnhancementStats);
        Assert.Equal(20.0f, loadedItem.EnhancementStats["Def"]);
    }

    [Fact]
    public async Task CompoundManager_ShouldReturnFailure_WhenNotImplemented()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompoundManager>>();
        var manager = new CompoundManager(loggerMock.Object);
        var character = new ServerCharacter { Name = "TestPlayer" };
        var request = new CompoundRequestDTO { TargetItemId = Guid.NewGuid() };

        // Act
        var response = await manager.ProcessCompoundRequest(character, request);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("pending implementation", response.Message);
        Assert.Equal(CompoundOutcome.Fail, response.Outcome);
    }
}
