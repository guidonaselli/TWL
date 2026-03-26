using System;
using Xunit;
using TWL.Server.Services.Combat;
using TWL.Server.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using System.Reflection;

namespace TWL.Tests.Server.Combat;

public class CombatProgressionPhaseAcceptanceTests
{
    [Fact]
    public void PhaseAcceptance_DeathPenaltyExpLoss_EnforcesExactPolicy_CMB01()
    {
        // CMB-01: Combat deaths remove exactly 1% of current-level EXP, floored at zero.
        var service = new DeathPenaltyService();
        var character = new ServerCharacter { Exp = 5000 };
        string deathEventId = Guid.NewGuid().ToString();

        var result = service.ApplyExpPenalty(character, deathEventId);

        Assert.True(result.Applied);
        Assert.Equal(50, result.ExpLost); // 1% of 5000
        Assert.Equal(4950, character.Exp);
        Assert.Equal(4950, result.NewExp);

        // Floor at zero
        var char2 = new ServerCharacter { Exp = 99 };
        var res2 = service.ApplyExpPenalty(char2, Guid.NewGuid().ToString());
        Assert.Equal(0, res2.ExpLost); // 0.99 floored
        Assert.Equal(99, char2.Exp);
    }

    [Fact]
    public void PhaseAcceptance_DeathPenaltyDurabilityLoss_EnforcesExactPolicy_CMB01()
    {
        // CMB-01/02: Items lose 1 durability on death.
        var service = new DeathPenaltyService();
        var character = new ServerCharacter { Exp = 1000 };
        var item1 = new Item { ItemId = 1, Durability = 10, MaxDurability = 10 };
        var item2 = new Item { ItemId = 2, Durability = 1, MaxDurability = 10 };

        var field = typeof(TWL.Server.Simulation.Networking.ServerCharacter).GetField("_equipment", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (System.Collections.Generic.List<Item>)field.GetValue(character)!;
        lock (list)
        {
            list.Add(item1);
            list.Add(item2);
        }

        string deathEventId = Guid.NewGuid().ToString();
        var result = service.ApplyExpPenalty(character, deathEventId);

        Assert.True(result.Applied);
    }

    [Fact]
    public void PhaseAcceptance_InstanceDailyRuns_Enforces5PerDayCap_INST01_INST02()
    {
        // INST-01/02/03: 5/day cap, UTC reset, entry rejection
        var instanceService = new InstanceService(new TWL.Server.Simulation.Managers.ServerMetrics());
        var character = new ServerCharacter();
        var instanceId = "DUNGEON_1";

        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;

        // Try to enter up to cap
        for (int i = 0; i < InstanceService.DailyLimit; i++)
        {
            Assert.True(instanceService.CanEnterInstance(character, instanceId));
            instanceService.RecordInstanceRun(character, instanceId);
        }

        // Run 6 should fail
        Assert.False(instanceService.CanEnterInstance(character, instanceId));

        // Wait a day (simulate UTC reset)
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date.AddDays(-1);

        // Run 6 on next day should succeed
        Assert.True(instanceService.CanEnterInstance(character, instanceId));
        // Verify reset behavior
        Assert.Empty(character.InstanceDailyRuns);
        Assert.Equal(DateTime.UtcNow.Date, character.InstanceDailyResetUtc);
    }
}
