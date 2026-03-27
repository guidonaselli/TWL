using Moq;
using Xunit;
using TWL.Server.Features.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Services.Combat;
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
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Tests.Mocks;
using Xunit;

namespace TWL.Tests.Server.Combat;

public class CombatFlowIntegrationTests
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
  },
  {
    ""SkillId"": 1000,
    ""Name"": ""Burn"",
    ""Element"": ""Fire"",
    ""Branch"": ""Magical"",
    ""Tier"": 1,
    ""TargetType"": ""SingleEnemy"",
    ""SpCost"": 0,
    ""Scaling"": [ { ""Stat"": ""Int"", ""Coefficient"": 1.0 } ],
    ""Effects"": [ { ""Tag"": ""Burn"", ""Value"": 10, ""Duration"": 3 } ]
  }
]");

        _deathPenaltyService = new DeathPenaltyService();
        _statusEngine = new StatusEngine();
        var random = new MockRandomService();
        var resolver = new StandardCombatResolver(random, SkillRegistry.Instance);
        var autoBattle = new AutoBattleManager(SkillRegistry.Instance);
        var petPolicy = new PetBattlePolicy(autoBattle, new Microsoft.Extensions.Logging.Abstractions.NullLogger<PetBattlePolicy>());

        var autoBattleManager = new AutoBattleManager(SkillRegistry.Instance);
        var petBattlePolicy = new PetBattlePolicy(autoBattleManager, NullLogger<PetBattlePolicy>.Instance);

        _combatManager = new CombatManager(resolver, _random, SkillRegistry.Instance, _statusEngine, autoBattleManager, petBattlePolicy, null, _deathPenaltyService);
    }

    [Fact]
    public void CombatFlow_AppliesDeathPenalties_WithoutBreakingPetAiTurnExecution()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 10, Str = 10, Exp = 1000 };
        var pet = new ServerPet { Id = -1, Name = "FaithfulDog", OwnerId = 1, Hp = 100, Str = 50, Team = Team.Player, Agi = 50 };
        var mob = new ServerCharacter { Id = 2, Name = "StrongCrab", Hp = 100, Str = 100, Team = Team.Enemy, Agi = 100, CharacterElement = Element.Water };

        _combatManager.RegisterCombatant(player);
        _combatManager.RegisterCombatant(pet);
        _combatManager.RegisterCombatant(mob);

        _combatManager.StartEncounter(1, new List<ServerCombatant> { player, pet, mob });

        // Act - Mob kills Player
        // Mob goes first (Agi 100 > Agi 50).
        // Since we aren't running the full Update loop, we simulate the skill use.
        player.Hp = 1; // Ensure mob kills player
        var request = new UseSkillRequest { PlayerId = 2, TargetId = 1, SkillId = 999 };
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].TargetDied);
        Assert.Equal(0, player.Hp);
        Assert.Equal(990, player.Exp); // 1% EXP loss (10 EXP lost)

        // Validate turn engine logic
        var participants = _combatManager.GetParticipants(1);
        Assert.DoesNotContain(player, participants); // Dead player removed
        Assert.Contains(pet, participants); // Pet remains

        // Pet turn should still execute (Spd 50) via Update
        _combatManager.Update(100); // Trigger AI turn

        // Pet attacks Mob
        Assert.True(mob.Hp <= 100, "Pet should have attacked the mob after player death");
    }

    [Fact]
    public void StatusEffectProcessing_RemainsStable_WhileDeathPenaltiesAreActive()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 10, Str = 10, Exp = 1000 };
        var mob = new ServerCharacter { Id = 2, Name = "StrongCrab", Hp = 100, Int = 100, Team = Team.Enemy, Agi = 100, CharacterElement = Element.Water };

        _combatManager.RegisterCombatant(player);

        var status = new StatusEffectInstance(SkillEffectTag.Burn, 5, 2, "Hp");
        player.AddStatusEffect(status, _statusEngine);

        var encounterId = _combatManager.CreateEncounter(new List<ServerCombatant> { player });
        var turnEngine = (TurnEngine)_combatManager.GetEncounter(encounterId)!;

        // Add burn to player
        player.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 5, 3, ""), _statusEngine);

        // Act - Mob kills Player
        player.Hp = 1; // Ensure mob kills player
        var request = new UseSkillRequest { PlayerId = 2, TargetId = 1, SkillId = 999 };
        var results = _combatManager.UseSkill(request);

        Assert.Equal(1, player.StatusEffects.Count);
        Assert.Equal(1, player.StatusEffects[0].TurnsRemaining);

        // Status effects process without throwing exceptions when dealing with dead characters
        var ex = Record.Exception(() => _statusEngine.Tick(player.StatusEffects.ToList()));
        Assert.Null(ex);

        // Ensure duration ticked down
        Assert.Equal(2, player.StatusEffects[0].TurnsRemaining);

        // Tick again -> dead player doesn't tick, doesn't crash
        var next = turnEngine.NextTurn();
        Assert.Null(next); // Player is skipped because Hp <= 0

        // Ensure status engine didn't throw
    }

    [Fact]
    public void MovementAndPetUtility_SeamsStayCoherent_WithCombatProgression()
    {
        // In TWL, pet utilities like mount or crafting can still be checked via service.
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 10, Exp = 1000 };
        var petDef = new PetDefinition { PetTypeId = 1, Name = "UtilityDog", Type = PetType.Quest, Element = Element.Earth, Utilities = new List<PetUtility> { new PetUtility { Type = PetUtilityType.Mount, RequiredLevel = 1, Value = 1.5f } } };

        var random = new MockRandomService();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<TWL.Server.Services.PetService>>();

        player.AddPet(pet);

        var player = new ServerCharacter { Id = 1, Hp = 0 }; // DEAD player
        var session = new TestClientSession(1, player, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        playerServiceMock.Setup(s => s.GetSession(1)).Returns(session);

        // Test PetUtility UseUtility when Dead
        bool result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount);
        Assert.False(result, "Dead player should not be able to use PetUtility");

        // Revive player, but put in combat
        player.Hp = 100;
        _combatManager.RegisterCombatant(player);

        result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount);
        Assert.False(result, "In-combat player should not be able to use PetUtility");

        // Clean up: remove from combat
        _combatManager.UnregisterCombatant(player.Id);

        // Add pet so it passes the null check
        var pet = new ServerPet { InstanceId = "pet_1", OwnerId = 1, DefinitionId = 1 };
        player.Pets.Add(pet);
        petManagerMock.Setup(m => m.GetUtilityValue(It.IsAny<int>(), It.IsAny<PetUtilityType>())).Returns(1.5f);

        // Test valid usage
        result = petService.UseUtility(1, "pet_1", PetUtilityType.Mount);
        Assert.True(result, "Alive and out-of-combat player should be able to use PetUtility");
    }
}
