using TWL.Shared.Domain.Battle;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

public class PetSystemTests : IDisposable
{
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<ILogger<PetService>> _mockLogger;
    private readonly PetManager _petManager;
    private readonly PetService _petService;

    private readonly PlayerService _playerService;

    public PetSystemTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_test.json", @"
[
  {
    ""PetTypeId"": 9999,
    ""Name"": ""Test Wolf"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 5,
    ""BaseWis"": 5,
    ""BaseAgi"": 10,
    ""CaptureRules"": {
        ""IsCapturable"": true,
        ""LevelLimit"": 1,
        ""BaseChance"": 0.5
    },
    ""GrowthModel"": {
      ""HpGrowthPerLevel"": 10.0,
      ""SpGrowthPerLevel"": 5.0
    },
    ""SkillSet"": [
        { ""SkillId"": 100, ""UnlockLevel"": 1, ""UnlockAmity"": 0 }
    ],
    ""RebirthEligible"": true,
    ""RebirthSkillId"": 900
  },
  {
    ""PetTypeId"": 8888,
    ""Name"": ""Temp Pet"",
    ""Type"": ""Quest"",
    ""Element"": ""Fire"",
    ""IsTemporary"": true,
    ""DurationSeconds"": 1
  }
]");
        _petManager.Load("Content/Data/pets_test.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();
        _mockLogger = new Mock<ILogger<PetService>>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object,
            _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_test.json"))
        {
            File.Delete("Content/Data/pets_test.json");
        }
    }

    [Fact]
    public void CapturePet_Success()
    {
        // Setup Session
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        // Define Enemy Monster
        var enemyMonsterDef = new MonsterDefinition
        {
            MonsterId = 200,
            Name = "Wild Wolf",
            Element = Element.Earth,
            IsCapturable = true,
            PetTypeId = 9999,
            CaptureThreshold = 0.5f
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(200)).Returns(enemyMonsterDef);

        var enemy = new ServerCharacter
        {
            Id = 200, // Runtime ID
            MonsterId = 200,
            Name = "Wild Wolf",
            Hp = 10,
            Con = 10,
            Team = Team.Enemy
        };
        _combatManager.RegisterCombatant(enemy);

        // Mock RNG
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.1f);

        // Act
        var petInstanceId = _petService.CaptureEnemy(1, 200);

        // Assert
        Assert.NotNull(petInstanceId);
        var pet = session.Character.Pets[0];
        Assert.Equal(9999, pet.DefinitionId);
        Assert.Equal(40, pet.Amity); // Default capture amity
    }

    [Fact]
    public void CapturePet_FailsIfSlotsFull()
    {
        // Setup Session
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1, Name = "Trainer" };
        // Fill slots
        for(int i=0; i<5; i++) chara.AddPet(new ServerPet { DefinitionId = 9999 });
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Mock Enemy
        var enemyMonsterDef = new MonsterDefinition
        {
            MonsterId = 200,
            IsCapturable = true,
            PetTypeId = 9999,
            CaptureThreshold = 1.0f
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(200)).Returns(enemyMonsterDef);

        var enemy = new ServerCharacter { Id = 200, MonsterId = 200, Hp = 10, Team = Team.Enemy };
        _combatManager.RegisterCombatant(enemy);
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.0f); // Always succeed

        // Act
        var result = _petService.CaptureEnemy(1, 200);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Amity_CombatWin_IncreasesAmity()
    {
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1 };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var petId = _petService.CreatePet(1, 9999);
        var pet = chara.Pets[0];
        pet.Amity = 50;

        // Act
        _petService.ProcessCombatWin(1, petId);

        // Assert
        Assert.Equal(51, pet.Amity);
    }

    [Fact]
    public void Amity_Death_DecreasesAmity()
    {
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1 };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var petId = _petService.CreatePet(1, 9999);
        var pet = chara.Pets[0];
        pet.Amity = 50;

        // Register in combat for event firing
        _combatManager.RegisterCombatant(pet);

        // Act - Simulate Death via CombatManager Event (or direct method call if simulated)
        // Since we mocked CombatManager events in PetService ctor, we need to trigger it.
        // But CombatManager is a class, not an interface mock here.
        // We can just call the handler via internal logic or simply simulate pet death logic directly if PetService handles it.
        // PetService subscribes to _combatManager.OnCombatantDeath.
        // Let's manually trigger it by killing the pet in CombatManager logic or invoking the event?
        // CombatManager has no method to invoke the event externally easily without damage.
        // So we damage it to 0.

        _mockResolver.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>()))
            .Returns(9999); // Kill it

        var attacker = new ServerCharacter { Id = 2, Team = Team.Enemy };
        _combatManager.RegisterCombatant(attacker);

        // Use a skill to kill it
        _mockSkills.Setup(s => s.GetSkillById(1)).Returns(new Skill { SkillId = 1, Effects = new List<SkillEffect>() });

        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 2, TargetId = pet.Id, SkillId = 1 });

        // Assert
        Assert.True(pet.IsDead);
        Assert.Equal(49, pet.Amity); // 50 - 1
    }

    [Fact]
    public void SwitchPet_ConsumesTurn_InCombat()
    {
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1 };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var pet1Id = _petService.CreatePet(1, 9999);
        var pet2Id = _petService.CreatePet(1, 9999);
        var pet1 = chara.Pets[0];
        var pet2 = chara.Pets[1];

        // Start with Pet 1 Active and In Combat
        chara.SetActivePet(pet1Id);
        pet1.EncounterId = 100; // In Combat
        _combatManager.RegisterCombatant(pet1);

        // Act
        var success = _petService.SwitchPet(1, pet2Id);

        // Assert
        Assert.True(success);
        Assert.Equal(pet2, chara.GetActivePet());
        Assert.Equal(100, pet2.EncounterId); // Should inherit encounter

        // Check Cooldown (Turn Consumed)
        // Pet 2 has skill 100. It should be on cooldown.
        Assert.True(pet2.IsSkillOnCooldown(100));
    }

    [Fact]
    public void Rebirth_ResetsLevel_UnlocksSkill()
    {
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1 };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        var petId = _petService.CreatePet(1, 9999);
        var pet = chara.Pets[0];

        // Setup for Rebirth
        pet.SetLevel(100);

        // Pre-assert
        Assert.DoesNotContain(900, pet.UnlockedSkillIds);

        // Act
        var success = _petService.TryRebirth(1, petId);

        // Assert
        Assert.True(success);
        Assert.True(pet.HasRebirthed);
        Assert.Equal(1, pet.Level);
        Assert.Contains(900, pet.UnlockedSkillIds);

        // Stats Check (Bonus)
        // Base Str 10. Rebirth +10% = 11?
        // At level 1, stats are recalculated.
        // BaseStr 10. Growth adds 0 at level 1.
        // Rebirth bonus +10%.
        // 10 * 1.1 = 11.
        Assert.Equal(11, pet.Str);
    }

    [Fact]
    public void QuestPet_Lifecycle_Expiry()
    {
        var session = new ClientSessionForTest();
        var chara = new ServerCharacter { Id = 1 };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Create Temp Pet (Duration 1s)
        var petId = _petService.CreatePet(1, 8888);
        var pet = chara.Pets[0];

        Assert.NotNull(pet.ExpirationTime);
        Assert.False(pet.IsExpired);

        // Wait 2s (Simulated via replacing DateTime or checking ExpirationTime value)
        // Since we can't easily mock DateTime.UtcNow in ServerPet without refactoring,
        // we manually modify ExpirationTime to be in the past.
        pet.ExpirationTime = DateTime.UtcNow.AddSeconds(-1);

        Assert.True(pet.IsExpired);

        // Try to revive (should fail)
        pet.Die();
        var revived = _petService.RevivePet(1, petId);
        Assert.False(revived);
    }
}

// Helper to expose setting Character
public class ClientSessionForTest : ClientSession
{
    public ClientSessionForTest()
    {
        UserId = 1;
    }

    public void SetCharacter(ServerCharacter c) => Character = c;
}
