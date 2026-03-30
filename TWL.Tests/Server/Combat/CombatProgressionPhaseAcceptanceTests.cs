using System;
using System.Collections.Generic;
using TWL.Server.Services;
using TWL.Server.Services.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Server.Combat;

public class CombatProgressionPhaseAcceptanceTests
{
    [Fact]
    public void Phase10_CMB01_DeathPenalty_AppliesCorrectly()
    {
        // Must-Have: "Acceptance tests validate exact policy values (1% EXP loss, -1 durability, 5/day cap, UTC reset)"
        // This checks CMB-01: Death Penalty

        // Arrange
        var deathPenaltyService = new DeathPenaltyService();
        var character = new ServerCharacter { Id = 1, Name = "Hero", Exp = 1000 };

        var item1 = new Item { ItemId = 1, MaxDurability = 100, Durability = 50 };
        var item2 = new Item { ItemId = 2, MaxDurability = 100, Durability = 1 }; // Will break
        var indestructibleItem = new Item { ItemId = 3, MaxDurability = 0, Durability = 0 };

        character.LoadSaveData(new TWL.Server.Persistence.ServerCharacterData
        {
            Exp = 1000,
            Equipment = new List<Item> { item1, item2, indestructibleItem }
        });

        // Act
        var result = deathPenaltyService.ApplyExpPenalty(character, Guid.NewGuid().ToString());

        // Assert
        Assert.True(result.Applied);
        Assert.Equal(10, result.ExpLost); // 1% of 1000
        Assert.Equal(990, character.Exp); // Floor check is handled inside DeathPenaltyServiceTests

        // Check Durability loss
        Assert.Equal(49, character.Equipment[0].Durability);
        Assert.Equal(0, character.Equipment[1].Durability);
        Assert.Equal(0, character.Equipment[2].Durability); // Indestructible

        Assert.False(character.Equipment[0].IsBroken);
        Assert.True(character.Equipment[1].IsBroken);
        Assert.False(character.Equipment[2].IsBroken);
    }

    [Fact]
    public void Phase10_INST01_InstanceLimit_AppliesCorrectly()
    {
        // Must-Have: "Acceptance tests validate exact policy values (1% EXP loss, -1 durability, 5/day cap, UTC reset)"
        // This checks INST-01, INST-02, INST-03: Instance limits

        // Arrange
        var instanceService = new InstanceService(new ServerMetrics());
        var character = new ServerCharacter();
        var instanceId = "TEST_INSTANCE_ACCEPTANCE";

        // Setup state to 4 runs today
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;
        character.InstanceDailyRuns[instanceId] = 4; // Max is 5

        // Act & Assert 1: Run 5 is allowed
        Assert.True(instanceService.CanEnterInstance(character, instanceId));
        instanceService.RecordInstanceRun(character, instanceId);

        // Act & Assert 2: Run 6 is denied
        Assert.False(instanceService.CanEnterInstance(character, instanceId));

        // Act & Assert 3: UTC Reset allows entry again
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date.AddDays(-1);
        Assert.True(instanceService.CanEnterInstance(character, instanceId));
    }
}
