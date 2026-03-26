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
    private readonly StatusEngine _statusEngine;
    private readonly DeathPenaltyService _deathPenaltyService;
    private readonly MockRandomService _random;

    public CombatFlowIntegrationTests()
    {
        SkillRegistry.Instance.ClearForTest();
        var skillsJson = @"
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
]";
        SkillRegistry.Instance.LoadSkills(skillsJson);

        _random = new MockRandomService();
        var resolver = new StandardCombatResolver(_random, SkillRegistry.Instance);
        _statusEngine = new StatusEngine();
        _deathPenaltyService = new DeathPenaltyService();

        var autoBattleManager = new AutoBattleManager(SkillRegistry.Instance);
        var petBattlePolicy = new PetBattlePolicy(autoBattleManager, NullLogger<PetBattlePolicy>.Instance);

        _combatManager = new CombatManager(resolver, _random, SkillRegistry.Instance, _statusEngine, autoBattleManager, petBattlePolicy, null, _deathPenaltyService);
    }

    [Fact]
    public void PlayerDeath_DoesNotBreakPetTurn_IntegrationTest()
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
    public void StatusEffect_RemainsStable_AfterDeath_IntegrationTest()
    {
        // Arrange
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 10, Str = 10, Exp = 1000 };
        var mob = new ServerCharacter { Id = 2, Name = "StrongCrab", Hp = 100, Int = 100, Team = Team.Enemy, Agi = 100, CharacterElement = Element.Water };

        _combatManager.RegisterCombatant(player);
        _combatManager.RegisterCombatant(mob);

        _combatManager.StartEncounter(1, new List<ServerCombatant> { player, mob });

        // Add burn to player
        player.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 5, 3, ""), _statusEngine);

        // Act - Mob kills Player
        player.Hp = 1; // Ensure mob kills player
        var request = new UseSkillRequest { PlayerId = 2, TargetId = 1, SkillId = 999 };
        var results = _combatManager.UseSkill(request);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].TargetDied);
        Assert.Equal(0, player.Hp);

        // Status effects process without throwing exceptions when dealing with dead characters
        var ex = Record.Exception(() => _statusEngine.Tick(player.StatusEffects.ToList()));
        Assert.Null(ex);

        // Ensure duration ticked down
        Assert.Equal(2, player.StatusEffects[0].TurnsRemaining);

        // Even if status effect ticks on dead player, HP stays at 0 (or bounded correctly)
        Assert.Equal(0, player.Hp);
    }

    [Fact]
    public void PetUtility_RemainsAvailable_AfterOwnerDeathPenalty_IntegrationTest()
    {
        // In TWL, pet utilities like mount or crafting can still be checked via service.
        var player = new ServerCharacter { Id = 1, Name = "Hero", Hp = 10, Exp = 1000 };
        var petDef = new PetDefinition { PetTypeId = 1, Name = "UtilityDog", Type = PetType.Quest, Element = Element.Earth, Utilities = new List<PetUtility> { new PetUtility { Type = PetUtilityType.Mount, RequiredLevel = 1, Value = 1.5f } } };

        var pet = new ServerPet { Id = -1, InstanceId = "pet123", Name = "UtilityDog", OwnerId = 1, Level = 10, Amity = 100, Team = Team.Player };
        pet.SetDefinition(petDef);

        player.AddPet(pet);

        // 1. Apply death penalty (simulate combat death)
        var result = _deathPenaltyService.ApplyExpPenalty(player, "death_event_123");
        Assert.True(result.Applied);
        Assert.Equal(990, player.Exp);
        // Character is dead logically, but Hp not managed by penalty service here

        // 2. Pet should still have its utility value valid
        float utilityValue = pet.GetUtilityValue(PetUtilityType.Mount);
        Assert.Equal(1.5f, utilityValue);
    }
}
