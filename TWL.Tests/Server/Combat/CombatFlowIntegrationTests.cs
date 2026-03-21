using System;
using System.Collections.Generic;
using Moq;
using TWL.Server.Services.Combat;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Server.Features.Combat;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Server.Combat;

public class CombatFlowIntegrationTests
{
    [Fact]
    public void CombatFlow_AppliesDeathPenalties_WithoutBreakingPetAiTurnExecution_AndClearsStatusEffects()
    {
        // 1. Arrange
        var mockResolver = new Mock<ICombatResolver>();
        var mockRandom = new Mock<IRandomService>();
        var mockSkills = new Mock<ISkillCatalog>();
        var statusEngine = new StatusEngine();
        var deathPenalty = new DeathPenaltyService();

        var skill = new Skill { SkillId = 1, SpCost = 0, Cooldown = 0 };
        mockSkills.Setup(s => s.GetSkillById(1)).Returns(skill);
        mockResolver.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>())).Returns(100);

        var combatManager = new CombatManager(mockResolver.Object, mockRandom.Object, mockSkills.Object, statusEngine, new AutoBattleManager(mockSkills.Object), null, deathPenalty);

        var player = new ServerCharacter { Id = 1, Name = "Player", Hp = 50, Exp = 1000, Team = Team.Player };
        var pet = new ServerPet { Id = 2, Name = "Pet", Hp = 100, Team = Team.Player, OwnerId = 1 };
        var enemy = new ServerPet { Id = 3, Name = "Enemy", Hp = 100, Team = Team.Enemy }; // Use ServerPet to mock ServerCombatant since it's abstract

        // Give player a status effect
        player.AddStatusEffect(new StatusEffectInstance(SkillEffectTag.Burn, 10, 3, "Burn"), statusEngine);
        Assert.Single(player.StatusEffects);

        combatManager.RegisterCombatant(player);
        combatManager.RegisterCombatant(pet);
        combatManager.RegisterCombatant(enemy);

        var engine = new TurnEngine(mockRandom.Object);
        engine.StartEncounter(new ServerCombatant[] { player, pet, enemy });

        var encounterId = 1;
        player.EncounterId = encounterId;
        pet.EncounterId = encounterId;
        enemy.EncounterId = encounterId;

        // Force sequence: Enemy, Pet, Player
        var field = typeof(TurnEngine).GetField("_turnQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var queue = (Queue<ServerCombatant>)field.GetValue(engine)!;
        queue.Clear();
        queue.Enqueue(enemy);
        queue.Enqueue(pet);
        queue.Enqueue(player);

        var encountersField = typeof(CombatManager).GetField("_encounters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (System.Collections.Concurrent.ConcurrentDictionary<int, ITurnEngine>)encountersField.GetValue(combatManager)!;
        dict[encounterId] = engine;

        // 2. Act
        engine.NextTurn(); // Set Enemy as current

        // Enemy attacks player and kills them
        combatManager.UseSkill(new UseSkillRequest { PlayerId = 3, TargetId = 1, SkillId = 1 });

        // 3. Assert
        Assert.Equal(0, player.Hp);
        Assert.Equal(990, player.Exp); // 1% of 1000 lost
        Assert.Empty(player.StatusEffects); // Statuses should be cleared

        // Advance time past 20 ticks for AI delay
        combatManager.Update(50);

        // Pet AI should take its turn, and the engine moves to Enemy
        Assert.Equal(enemy.Id, engine.CurrentCombatant!.Id);
    }

    [Fact]
    public void UseUtility_BlockedWhenInCombat_OrDead()
    {
        var mockPlayerService = new Mock<TWL.Server.Services.IPlayerService>();
        var mockCombatManager = new Mock<TWL.Server.Simulation.Managers.CombatManager>(new Mock<ICombatResolver>().Object, new Mock<IRandomService>().Object, new Mock<ISkillCatalog>().Object, new Mock<IStatusEngine>().Object, new AutoBattleManager(new Mock<ISkillCatalog>().Object), null, null);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TWL.Server.Services.PetService>>();
        var mockPetManager = new Mock<TWL.Server.Services.IPetManager>();
        var mockMonsterManager = new Mock<TWL.Server.Services.IMonsterManager>();
        var mockRandom = new Mock<IRandomService>();

        var petService = new TWL.Server.Services.PetService(
            mockPlayerService.Object,
            mockPetManager.Object,
            mockMonsterManager.Object,
            mockCombatManager.Object,
            mockRandom.Object,
            mockLogger.Object
        );

        var player = new ServerCharacter { Id = 1, Hp = 100, EncounterId = 1 }; // In combat
        var pet = new ServerPet { Id = 2, OwnerId = 1, InstanceId = "pet1" };
        player.AddPet(pet);

        var session = new Mock<TWL.Server.Simulation.Networking.ClientSessionForTest>();
        session.Setup(s => s.Character).Returns(player);
        mockPlayerService.Setup(ps => ps.GetSession(1)).Returns(session.Object);

        // Attempt Mount
        var result = petService.UseUtility(1, "pet1", PetUtilityType.Mount);
        Assert.False(result); // Blocked by combat

        // Set not in combat, but dead
        player.EncounterId = 0;
        player.Hp = 0;

        result = petService.UseUtility(1, "pet1", PetUtilityType.Mount);
        Assert.False(result); // Blocked by death
    }
}
