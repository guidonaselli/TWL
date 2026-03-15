using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Compound;

public class CompoundPhaseAcceptanceTests
{
    private readonly Mock<ICompoundRatePolicy> _ratePolicyMock;
    private readonly Mock<IRandomService> _randomServiceMock;
    private readonly CompoundManager _compoundManager;

    public CompoundPhaseAcceptanceTests()
    {
        _ratePolicyMock = new Mock<ICompoundRatePolicy>();
        _randomServiceMock = new Mock<IRandomService>();
        _compoundManager = new CompoundManager(NullLogger<CompoundManager>.Instance, _ratePolicyMock.Object, _randomServiceMock.Object);
    }

    [Fact]
    public async Task Phase8_FullFlow_Integration()
    {
        // 1. Arrange: Setup character with items
        var character = new ServerCharacter { Id = 1, Name = "AcceptanceTester" };
        var targetId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        
        var targetItem = new Item { ItemId = 1001, InstanceId = targetId, Name = "Iron Sword", EnhancementLevel = 0 };
        var ingredientItem = new Item { ItemId = 2001, InstanceId = ingredientId, Name = "Iron Ore", Quantity = 1 };
        
        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData
        {
            Inventory = new List<Item> { targetItem, ingredientItem }
        });

        var request = new CompoundRequestDTO
        {
            TargetItemId = targetId,
            IngredientItemId = ingredientId
        };

        // 2. Setup Deterministic Outcome: Success
        _ratePolicyMock.Setup(p => p.GetSuccessChance(It.IsAny<Item>(), It.IsAny<Item>())).Returns(1.0); // 100%
        _randomServiceMock.Setup(r => r.NextDouble(It.IsAny<string>())).Returns(0.5);

        // 3. Act: Process Request
        var response = await _compoundManager.ProcessCompoundRequest(character, request);

        // 4. Assert: Verify end-to-end outcome
        Assert.True(response.Success);
        Assert.Equal(CompoundOutcome.Success, response.Outcome);
        Assert.Equal(1, response.NewEnhancementLevel);
        
        // Verify inventory state
        Assert.Equal(1, character.Inventory.Count);
        var updatedTarget = character.Inventory.First();
        Assert.Equal(targetId, updatedTarget.InstanceId);
        Assert.Equal(1, updatedTarget.EnhancementLevel);
        Assert.False(character.Inventory.Any(i => i.InstanceId == ingredientId));
    }

    [Fact]
    public async Task Phase8_Persistence_Mock_Check()
    {
        // Verify that enhancement data is included in save data
        var character = new ServerCharacter { Id = 1 };
        var item = new Item { ItemId = 1001, EnhancementLevel = 5 };
        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData { Inventory = new List<Item> { item } });
        
        var saveData = character.GetSaveData();
        
        Assert.Equal(5, saveData.Inventory[0].EnhancementLevel);
    }
}
