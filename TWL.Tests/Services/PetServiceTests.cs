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
            Con = 10,
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

        _mockRandom.Setup(r => r.NextFloat()).Returns(0.1f);

        // Act
        var result = _petService.CaptureEnemy(ownerId, enemyId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(character.Pets);
        Assert.Equal(petTypeId, character.Pets[0].DefinitionId);
        Assert.Equal(0, enemy.Hp);
    }

    [Fact]
    public void SwitchPet_Success_And_CorrectID()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, GrowthModel = new PetGrowthModel() };
        var pet = new ServerPet(petDef);
        character.AddPet(pet);

        // Act
        bool result = _petService.SwitchPet(ownerId, pet.InstanceId);

        // Assert
        Assert.True(result);
        Assert.Equal(pet.InstanceId, character.ActivePetInstanceId);

        // Verify the registered ID is -OwnerId
        _mockCombatManager.Verify(cm => cm.RegisterCombatant(It.Is<ServerCombatant>(sc => sc.Id == -ownerId)), Times.Once);

        // Ensure name is correct
        Assert.Equal(pet.Name, character.GetActivePet().Name);
    }

    [Fact]
    public void SwitchPet_Dismiss_Active()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, GrowthModel = new PetGrowthModel() };
        var pet1 = new ServerPet(petDef);
        var pet2 = new ServerPet(petDef);
        character.AddPet(pet1);
        character.AddPet(pet2);

        // Set Pet1 Active
        character.SetActivePet(pet1.InstanceId);
        pet1.Id = -ownerId; // Simulate existing active pet ID

        // Act - Switch to Pet2
        bool result = _petService.SwitchPet(ownerId, pet2.InstanceId);

        // Assert
        Assert.True(result);
        Assert.Equal(pet2.InstanceId, character.ActivePetInstanceId);

        // Verify unregistering old pet ID
        _mockCombatManager.Verify(cm => cm.UnregisterCombatant(-ownerId), Times.Once);
        // Verify registering new pet with same ID
        _mockCombatManager.Verify(cm => cm.RegisterCombatant(It.Is<ServerCombatant>(sc => sc.Id == -ownerId && (sc as ServerPet).InstanceId == pet2.InstanceId)), Times.Once);
    }

    [Fact]
    public void DismissPet_Success()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, GrowthModel = new PetGrowthModel() };
        var pet = new ServerPet(petDef);
        character.AddPet(pet);
        character.SetActivePet(pet.InstanceId);

        // Act
        bool result = _petService.DismissPet(ownerId, pet.InstanceId);

        // Assert
        Assert.True(result);
        Assert.Empty(character.Pets);
        Assert.Null(character.ActivePetInstanceId);

        // Verify removal from combat
        // Note: DismissPet might need to know the runtime ID of the pet to unregister it.
        // If the pet was active, it should unregister -ownerId
        _mockCombatManager.Verify(cm => cm.UnregisterCombatant(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void ModifyAmity_PenaltyEffect()
    {
        // Arrange
        int ownerId = 1;
        var character = new ServerCharacter { Id = ownerId };
        var session = new TestClientSession(character);
        _mockPlayerService.Setup(p => p.GetSession(ownerId)).Returns(session);

        var petDef = new PetDefinition { PetTypeId = 1, BaseStr = 10, GrowthModel = new PetGrowthModel() };
        var pet = new ServerPet(petDef);
        pet.Amity = 50;
        character.AddPet(pet);

        int initialStr = pet.Str;

        // Act - Reduce Amity below 20
        _petService.ModifyAmity(ownerId, pet.InstanceId, -40); // 50 - 40 = 10

        // Assert
        Assert.Equal(10, pet.Amity);
        Assert.True(pet.IsRebellious);
        Assert.True(pet.Str < initialStr); // Should be reduced
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
}
