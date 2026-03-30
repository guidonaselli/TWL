using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Features.Combat;
using TWL.Server.Services;
using TWL.Server.Services.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Server.Combat;

public class CombatProgressionPhaseAcceptanceTests
{
    public CombatProgressionPhaseAcceptanceTests()
    {
        SkillRegistry.Instance.ClearForTest();
        SkillRegistry.Instance.LoadSkills(@"
[
  {
    ""SkillId"": 999,
    ""Name"": ""Basic Attack"",
    ""Element"": ""Earth"",
    ""Branch"": ""Physical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 0,
    ""Scaling"": [ { ""Stat"": ""Str"", ""Coefficient"": 2.0 } ],
    ""Effects"": [ { ""Tag"": ""Damage"" } ]
  }
]");
    }

    [Fact]
    public void CMB_01_PlayerDeath_AppliesPenalty()
    {
        // CMB-01: Death-penalty EXP loss (1%) and durability loss (-1)
        var deathPenaltyService = new DeathPenaltyService();
        var character = new ServerCharacter { Id = 1, Exp = 1000, Hp = 0 };
        var data = new TWL.Server.Persistence.ServerCharacterData { Equipment = new List<Item> { new Item { ItemId = 1, MaxDurability = 100, Durability = 100 } } };
        character.LoadSaveData(data);

        var result = deathPenaltyService.ApplyExpPenalty(character, "cmb_01_test");

        Assert.True(result.Applied);
        Assert.Equal(990, character.Exp);
        Assert.Equal(99, character.Equipment[0].Durability);
    }

    [Fact]
    public void INST_01_02_03_InstanceLimits_Enforced()
    {
        // INST-01/02/03: Max 5 runs per day, resets at UTC midnight, entry blocked if at cap
        var instanceService = new InstanceService(new ServerMetrics());
        var character = new ServerCharacter();
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;

        for (int i = 0; i < 5; i++)
        {
            Assert.True(instanceService.CanEnterInstance(character, "test_instance"));
            instanceService.RecordInstanceRun(character, "test_instance");
        }

        Assert.False(instanceService.CanEnterInstance(character, "test_instance"));

        // Simulate new day
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date.AddDays(-1);
        Assert.True(instanceService.CanEnterInstance(character, "test_instance"));
    }
}
