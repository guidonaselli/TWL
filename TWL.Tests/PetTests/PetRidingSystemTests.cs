using Xunit;
using TWL.Server.Persistence.Services;
using TWL.Server.Persistence;
using TWL.Server.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Server.Simulation.Managers;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Shared.Services;
using TWL.Server.Architecture.Observability;

namespace TWL.Tests.PetTests;

public class PetRidingSystemTests
{
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly Mock<PetManager> _petManagerMock;
    private readonly Mock<MonsterManager> _monsterManagerMock;
    private readonly Mock<CombatManager> _combatManagerMock;
    private readonly Mock<IRandomService> _randomMock;
    private readonly Mock<ILogger<PetService>> _loggerMock;
    private readonly PetService _petService;

    public PetRidingSystemTests()
    {
        _playerServiceMock = new Mock<PlayerService>(new Mock<IPlayerRepository>().Object, new Mock<ServerMetrics>().Object);
        _petManagerMock = new Mock<PetManager>();
        _monsterManagerMock = new Mock<MonsterManager>();
        _combatManagerMock = new Mock<CombatManager>(
            new Mock<ICombatResolver>().Object,
            new Mock<IRandomService>().Object,
            new Mock<ISkillCatalog>().Object,
            new Mock<IStatusEngine>().Object,
            null
        );
        _randomMock = new Mock<IRandomService>();
        _loggerMock = new Mock<ILogger<PetService>>();

        _petService = new PetService(
            _playerServiceMock.Object,
            _petManagerMock.Object,
            _monsterManagerMock.Object,
            _combatManagerMock.Object,
            _randomMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public void UseUtility_Mount_TogglesMountStateAndSpeed()
    {
        // Arrange
        var ownerId = 1;
        var petId = "pet123";
        var character = new ServerCharacter { Id = ownerId };
        var sessionMock = new Mock<ClientSession>();
        sessionMock.SetupGet(s => s.Character).Returns(character);
        _playerServiceMock.Setup(p => p.GetSession(ownerId)).Returns(sessionMock.Object);

        var petDef = new PetDefinition { PetTypeId = 1001, Name = "Horse", Element = Element.Earth, Utilities = new List<PetUtility> { new PetUtility { Type = PetUtilityType.Mount, Value = 0.5f } } };
        var pet = new ServerPet(petDef) { InstanceId = petId };
        character.AddPet(pet);

        // Act - Mount
        var success = _petService.UseUtility(ownerId, petId, PetUtilityType.Mount);

        // Assert
        Assert.True(success);
        Assert.True(character.IsMounted);
        Assert.Equal(1.5f, character.MoveSpeedModifier);

        // Act - Unmount
        success = _petService.UseUtility(ownerId, petId, PetUtilityType.Mount);

        // Assert
        Assert.True(success);
        Assert.False(character.IsMounted);
        Assert.Equal(1.0f, character.MoveSpeedModifier);
    }

    [Fact]
    public void UseUtility_NonMountablePet_ReturnsFalse()
    {
        // Arrange
        var ownerId = 1;
        var petId = "pet123";
        var character = new ServerCharacter { Id = ownerId };
        var sessionMock = new Mock<ClientSession>();
        sessionMock.SetupGet(s => s.Character).Returns(character);
        _playerServiceMock.Setup(p => p.GetSession(ownerId)).Returns(sessionMock.Object);

        var petDef = new PetDefinition { PetTypeId = 1002, Name = "Cat", Element = Element.Wind, Utilities = new List<PetUtility>() };
        var pet = new ServerPet(petDef) { InstanceId = petId };
        character.AddPet(pet);

        // Act
        var success = _petService.UseUtility(ownerId, petId, PetUtilityType.Mount);

        // Assert
        Assert.False(success);
        Assert.False(character.IsMounted);
    }
}
