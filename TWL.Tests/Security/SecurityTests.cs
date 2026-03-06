using System.Numerics;
using Moq;
using TWL.Server.Features.Interactions;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using Xunit;

namespace TWL.Tests.Security;

public class SecurityTests
{
    [Fact]
    public async Task InteractRequest_OutOfRange_ShouldReject()
    {
        var mockManager = new Mock<InteractionManager>();
        mockManager.Setup(m => m.GetTargetPosition(1, "RareChest")).Returns(new Vector2(1000f, 1000f));

        var handler = new InteractHandler(mockManager.Object);
        var character = new ServerCharacter { X = 0, Y = 0, Id = 1, MapId = 1 };

        // Ensure quest component avoids nullrefs if needed
        var questComponent = new PlayerQuestComponent(null, null);

        var command = new InteractCommand(character, questComponent, "RareChest");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InteractRequest_InRange_ShouldSucceed()
    {
        var mockManager = new Mock<InteractionManager>();
        mockManager.Setup(m => m.GetTargetPosition(1, "RareChest")).Returns(new Vector2(10f, 10f));
        mockManager.Setup(m => m.ProcessInteraction(It.IsAny<ServerCharacter>(), It.IsAny<PlayerQuestComponent>(), It.IsAny<string>())).Returns("Gather");

        var handler = new InteractHandler(mockManager.Object);
        var character = new ServerCharacter { X = 0, Y = 0, Id = 1, MapId = 1 };

        var questComponent = new PlayerQuestComponent(null, null);

        var command = new InteractCommand(character, questComponent, "RareChest");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
    }
}
