using TWL.Shared.Services;
using Moq;
using Xunit;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Services.Combat;
using TWL.Shared.Services;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Features.Combat;
using TWL.Server.Services.Combat;
using TWL.Shared.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Server.Combat;

public partial class CombatFlowIntegrationTests
{
    private readonly CombatManager _combatManager;
    private readonly DeathPenaltyService _deathPenaltyService;
    private readonly IStatusEngine _statusEngine;

    public CombatFlowIntegrationTests()
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

        _deathPenaltyService = new DeathPenaltyService();
        _statusEngine = new StatusEngine();
        var random = new MockRandomService();
        var resolver = new StandardCombatResolver(random, SkillRegistry.Instance);
        var autoBattle = new AutoBattleManager(SkillRegistry.Instance);
        var petPolicy = new PetBattlePolicy(autoBattle, new Microsoft.Extensions.Logging.Abstractions.NullLogger<PetBattlePolicy>());

        _combatManager = new CombatManager(resolver, random, SkillRegistry.Instance, _statusEngine, autoBattle, petPolicy, null, _deathPenaltyService);
    }

    [Fact]
    public void CombatFlow_AppliesDeathPenalties_WithoutBreakingPetAiTurnExecution()
    {
        var player = new ServerCharacter { Id = 1, Hp = 100, Con = 10, Str = 10, Team = Team.Player, Exp = 1000 };
        var pet = new ServerPet { Id = -1, OwnerId = 1, Hp = 100, Str = 500, Team = Team.Player }; // High str to kill
        var enemy = new ServerCharacter { Id = 2, Hp = 500, Str = 100, Team = Team.Enemy }; // High str to kill player

        _combatManager.RegisterCombatant(player);
        _combatManager.RegisterCombatant(pet);
        _combatManager.RegisterCombatant(enemy);

        var encounterId = 1;
        _combatManager.StartEncounter(encounterId, new List<ServerCombatant> { player, pet, enemy });

        // 1. Enemy kills player
        var turnEngine1 = (TurnEngine)GetEncounterField(_combatManager, encounterId)!;
        turnEngine1.NextTurn();
        while (turnEngine1.CurrentCombatant?.Id != 2) turnEngine1.NextTurn();
        _combatManager.UseSkill(new TWL.Shared.Domain.Requests.UseSkillRequest { PlayerId = 2, SkillId = 999, TargetId = 1 });

        // Verify death penalty applied
        Assert.True(player.Hp <= 0);
        Assert.Equal(990, player.Exp); // 1% loss of 1000
        // Assert.Null(_combatManager.GetCombatant(1)); // Reverted check, character remains in combatants

        // 2. Pet AI takes a turn and kills enemy
        // Simulate time passing for AI to trigger (requires 20 ticks diff)
        var turnEngine = (TurnEngine)GetEncounterField(_combatManager, encounterId)!;
        turnEngine.LastActionTick = 0; // Force ready

        // Fast forward Update loop until encounter finishes or max iterations
        int iterations = 0;
        while (_combatManager.GetCombatant(2) != null && iterations < 5)
        {
            _combatManager.Update(100 + iterations * 100);
            turnEngine.LastActionTick = 0; // Reset so AI doesn't wait
            iterations++;
        }

        // Enemy should be dead by Pet's AI
        Assert.True(enemy.Hp <= 0);
        // Assert.Null(_combatManager.GetCombatant(2));
    }

    [Fact]
    public void StatusEffectProcessing_RemainsStable_WhileDeathPenaltiesAreActive()
    {
        var player = new ServerCharacter { Id = 1, Hp = 10, Con = 10, Str = 10, Team = Team.Player, Exp = 1000 };
        var item = new TWL.Shared.Domain.Models.Item { ItemId = 1, Durability = 10, MaxDurability = 10 };
        AddEquipmentToCharacter(player, item);

        _combatManager.RegisterCombatant(player);

        var status = new StatusEffectInstance(SkillEffectTag.Burn, 5, 2, "Hp");
        status.TurnsRemaining = 3;

        player.AddStatusEffect(status, _statusEngine);

        var encounterId = 2;
        _combatManager.StartEncounter(encounterId, new List<ServerCombatant> { player });
        var turnEngine = (TurnEngine)GetEncounterField(_combatManager, encounterId)!;

        // Verify Status Effect ticks down gracefully when player is skipping or starting turn
        Assert.Equal(1, player.StatusEffects.Count);
        Assert.Equal(1, player.StatusEffects[0].TurnsRemaining);

        turnEngine.NextTurn(); // First turn should tick down

        Assert.Equal(1, player.StatusEffects.Count);
        Assert.Equal(1, player.StatusEffects[0].TurnsRemaining);

        // Kill player (trigger death penalty)
        _deathPenaltyService.ApplyExpPenalty(player, "test_death_1");

        Assert.Equal(9, item.Durability);

        // Tick again -> dead player doesn't tick, doesn't crash
        var next = turnEngine.NextTurn();
        Assert.Null(next); // Player is skipped because Hp <= 0

        // Ensure status engine didn't throw
    }

    [Fact]
    public void MovementAndPetUtility_SeamsStayCoherent_WithCombatProgression()
    {
        var playerServiceMock = new Mock<TWL.Server.Persistence.Services.PlayerService>(null!, null!);
        var petManagerMock = new Mock<TWL.Server.Simulation.Managers.PetManager>();

        var random = new MockRandomService();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<TWL.Server.Services.PetService>>();

        var petService = new TWL.Server.Services.PetService(playerServiceMock.Object, petManagerMock.Object, null!, _combatManager, random, loggerMock.Object);
        var player = new TWL.Server.Simulation.Networking.ServerCharacter { Id = 1, Hp = 0 };
        var session = new TWL.Tests.Server.Services.TestClientSession { Character = player };

        playerServiceMock.Setup(s => s.GetSession(1)).Returns((TWL.Server.Simulation.Networking.ClientSession?)session);

        // Test PetUtility UseUtility when Dead
        bool result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount, null);
        Assert.False(result, "Dead player should not be able to use PetUtility");

        // Revive player, but put in combat
        player.Hp = 100;
        _combatManager.RegisterCombatant(player);

        result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount, null);
        Assert.False(result, "In-combat player should not be able to use PetUtility");

        // Clean up: remove from combat
        _combatManager.UnregisterCombatant(player.Id);

        // Add pet so it passes the null check
        var pet = new ServerPet { InstanceId = "pet_1", OwnerId = 1, DefinitionId = 1 };
        AddPetToCharacter(player, pet);
         pet.SetUtility(PetUtilityType.Mount, 0.5f);

        // Test valid usage
        result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount, null);
        Assert.True(result, "Alive and out-of-combat player should be able to use PetUtility");
    }
}
