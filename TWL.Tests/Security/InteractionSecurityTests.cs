using Moq;
using TWL.Server.Features.Interactions;
using TWL.Server.Simulation.Managers;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using Xunit;

namespace TWL.Tests.Security;

public class InteractionSecurityTests
{
    [Fact]
    public async Task InteractRequest_OutOfRange_ShouldReject()
    {
        // Arrange
        var interactionManager = new InteractionManager();
        var mapRegistryMock = new Mock<IMapRegistry>();

        // Set up the map registry to return a specific position for the target
        mapRegistryMock.Setup(m => m.GetEntityPosition(1, "TestNPC"))
            .Returns((500f, 500f)); // Entity is at 500, 500

        var handler = new InteractHandler(interactionManager, mapRegistryMock.Object);

        // Character is at 0, 0 which is far from 500, 500
        var character = new ServerCharacter
        {
            Id = 1,
            MapId = 1,
            X = 0f,
            Y = 0f
        };
        var questComponent = new PlayerQuestComponent(new ServerQuestManager()) { Character = character };

        var command = new InteractCommand(character, questComponent, "TestNPC");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success, "Interaction should be rejected because character is out of range.");
    }

    [Fact]
    public async Task InteractRequest_InRange_ShouldSucceed()
    {
        // Arrange
        var interactionManager = new InteractionManager();
        var mapRegistryMock = new Mock<IMapRegistry>();

        // Set up the map registry to return a specific position for the target
        mapRegistryMock.Setup(m => m.GetEntityPosition(1, "TestNPC"))
            .Returns((50f, 50f)); // Entity is at 50, 50

        var handler = new InteractHandler(interactionManager, mapRegistryMock.Object);

        // Character is at 40, 40 which is within 160 units
        var character = new ServerCharacter
        {
            Id = 1,
            MapId = 1,
            X = 40f,
            Y = 40f
        };
        var questComponent = new PlayerQuestComponent(new ServerQuestManager()) { Character = character };

        var command = new InteractCommand(character, questComponent, "TestNPC");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success, "Interaction should succeed because character is in range.");
    }
}
