using System.Collections.Generic;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Services;

public class TestClientSession : ClientSession
{
    public TestClientSession(ServerCharacter character)
    {
        Character = character;
        UserId = character.Id;
    }
}

public class PetServiceTests
{
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly Mock<PetManager> _mockPetManager;
    private readonly Mock<CombatManager> _mockCombatManager;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly PetService _petService;

    public PetServiceTests()
    {
        var mockRepo = new Mock<IPlayerRepository>();
        var mockMetrics = new Mock<ServerMetrics>();
        _mockPlayerService = new Mock<PlayerService>(mockRepo.Object, mockMetrics.Object);

        _mockPetManager = new Mock<PetManager>();

        var mockResolver = new Mock<ICombatResolver>();
        var mockStatus = new Mock<IStatusEngine>();
        var mockCatalog = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();

        _mockCombatManager = new Mock<CombatManager>(mockResolver.Object, _mockRandom.Object, mockCatalog.Object, mockStatus.Object);

        _petService = new PetService(
            _mockPlayerService.Object,
            _mockPetManager.Object,
            _mockCombatManager.Object,
            _mockRandom.Object
        );
    }

    private void LevelUpCharacter(ServerCharacter character, int targetLevel)
    {
        // Simple hack to level up
        while(character.Level < targetLevel)
        {
            character.AddExp(character.ExpToNextLevel);
        }
    }

    [Fact]
    public void CaptureEnemy_Success()
    {
        // Arrange
        int ownerId = 1;
        int enemyId = 100;
        int petTypeId = 1001;

        var character = new ServerCharacter { Id = ownerId };
        LevelUpCharacter(character, 10);

        var session = new TestClientSession(character);

        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var enemyDef = new EnemyCharacter("Wolf", Element.Earth, true)
        {
            PetTypeId = petTypeId,
            CaptureThreshold = 0.5f,
            Level = 5,
            Health = 20,
            Con = 10, // MaxHp = 100
            MaxHealth = 100
        };

        var enemy = new ServerEnemy(enemyDef) { Id = enemyId, Hp = 20 };
        _mockCombatManager.Setup(c => c.GetCombatant(enemyId)).Returns(enemy);

        var petDef = new PetDefinition
        {
            PetTypeId = petTypeId,
            Name = "Wolf Pet",
            CaptureRules = new CaptureRules
            {
                IsCapturable = true,
                LevelLimit = 5,
                BaseChance = 0.5f
            },
            GrowthModel = new PetGrowthModel()
        };
        _mockPetManager.Setup(pm => pm.GetDefinition(petTypeId)).Returns(petDef);

        // Mock Random to succeed
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.1f);

        // Act
        var result = _petService.CaptureEnemy(ownerId, enemyId);

        // Assert
        Assert.NotNull(result); // InstanceId
        Assert.Single(character.Pets);
        Assert.Equal(petTypeId, character.Pets[0].DefinitionId);
        Assert.Equal(0, enemy.Hp); // Enemy died
    }

    [Fact]
    public void CaptureEnemy_Fail_Threshold()
    {
        // Arrange
        int ownerId = 1;
        int enemyId = 100;

        var character = new ServerCharacter { Id = ownerId };
        LevelUpCharacter(character, 10);
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var enemyDef = new EnemyCharacter("Wolf", Element.Earth, true)
        {
            PetTypeId = 1001,
            CaptureThreshold = 0.1f, // Strict threshold
            MaxHealth = 100,
            Health = 50 // 50% > 10%
        };
        var enemy = new ServerEnemy(enemyDef) { Id = enemyId, Hp = 50 };
        _mockCombatManager.Setup(c => c.GetCombatant(enemyId)).Returns(enemy);

        // Act
        var result = _petService.CaptureEnemy(ownerId, enemyId);

        // Assert
        Assert.Null(result);
        Assert.Empty(character.Pets);
    }

    [Fact]
    public void CaptureEnemy_Fail_Dead()
    {
        // Arrange
        int ownerId = 1;
        int enemyId = 100;

        var character = new ServerCharacter { Id = ownerId };
        LevelUpCharacter(character, 10);
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var enemyDef = new EnemyCharacter("Wolf", Element.Earth, true)
        {
            PetTypeId = 1001,
            CaptureThreshold = 0.5f,
            MaxHealth = 100,
            Health = 0 // DEAD
        };
        var enemy = new ServerEnemy(enemyDef) { Id = enemyId, Hp = 0 }; // DEAD
        _mockCombatManager.Setup(c => c.GetCombatant(enemyId)).Returns(enemy);

        // Act
        var result = _petService.CaptureEnemy(ownerId, enemyId);

        // Assert
        Assert.Null(result);
        Assert.Empty(character.Pets);
    }

    [Fact]
    public void CaptureEnemy_Fail_Level()
    {
        // Arrange
        int ownerId = 1;
        int enemyId = 100;
        int petTypeId = 1001;

        var character = new ServerCharacter { Id = ownerId }; // Level 1
        // LevelUpCharacter(character, 1);
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var enemyDef = new EnemyCharacter("Wolf", Element.Earth, true)
        {
            PetTypeId = petTypeId,
            CaptureThreshold = 0.5f,
            MaxHealth = 100,
            Health = 10
        };
        var enemy = new ServerEnemy(enemyDef) { Id = enemyId, Hp = 10 };
        _mockCombatManager.Setup(c => c.GetCombatant(enemyId)).Returns(enemy);

        var petDef = new PetDefinition
        {
            PetTypeId = petTypeId,
            CaptureRules = new CaptureRules
            {
                IsCapturable = true,
                LevelLimit = 5, // Requires level 5
                BaseChance = 0.5f
            }
        };
        _mockPetManager.Setup(pm => pm.GetDefinition(petTypeId)).Returns(petDef);

        // Act
        var result = _petService.CaptureEnemy(ownerId, enemyId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RevivePet_Success()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, Name = "DeadPet", GrowthModel = new PetGrowthModel() };
        var pet = new ServerPet(petDef);
        pet.Die(); // Make dead
        character.AddPet(pet);

        // Act
        bool result = _petService.RevivePet(ownerId, pet.InstanceId);

        // Assert
        Assert.True(result);
        Assert.False(pet.IsDead);
        Assert.Equal(pet.MaxHp, pet.Hp);
    }

    [Fact]
    public void ModifyAmity_Clamps()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, GrowthModel = new PetGrowthModel() };
        var pet = new ServerPet(petDef);
        pet.Amity = 50;
        character.AddPet(pet);

        // Act
        _petService.ModifyAmity(ownerId, pet.InstanceId, 100); // Should cap at 100

        // Assert
        Assert.Equal(100, pet.Amity);

        // Act 2
        _petService.ModifyAmity(ownerId, pet.InstanceId, -200); // Should floor at 0

        // Assert
        Assert.Equal(0, pet.Amity);
    }
}
