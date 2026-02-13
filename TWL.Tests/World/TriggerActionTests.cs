using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Actions;
using TWL.Server.Services.World.Actions.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.World;

public class TriggerActionTests
{
    [Fact]
    public void Registry_ShouldRegisterAndRetrieveHandler()
    {
        var registry = new TriggerActionRegistry();
        var handler = new TeleportActionHandler();

        registry.Register(handler);
        var retrieved = registry.GetHandler("Teleport");

        Assert.NotNull(retrieved);
        Assert.Same(handler, retrieved);
        Assert.Equal("Teleport", retrieved.ActionType);
    }

    [Fact]
    public void TeleportAction_ShouldTeleportCharacter()
    {
        var handler = new TeleportActionHandler();
        var character = new ServerCharacter { MapId = 1, X = 100, Y = 100 };
        var action = new TriggerAction
        {
            Type = "Teleport",
            Parameters = new Dictionary<string, string>
            {
                { "MapId", "2" },
                { "X", "200" },
                { "Y", "200" }
            }
        };

        handler.Execute(character, action);

        Assert.Equal(2, character.MapId);
        Assert.Equal(200, character.X);
        Assert.Equal(200, character.Y);
    }

    [Fact]
    public void SpawnAction_ShouldStartScriptedEncounter()
    {
        var mockPlayerService = new Mock<PlayerService>(null, null);
        var mockSpawnManager = new Mock<SpawnManager>(null, null, null, null);
        var mockSession = new Mock<ClientSession>();

        mockPlayerService.Setup(s => s.GetSession(It.IsAny<int>())).Returns(mockSession.Object);
        mockSpawnManager.Setup(s => s.StartScriptedEncounter(It.IsAny<ClientSession>(), It.IsAny<int>(), It.IsAny<int>()));

        var handler = new SpawnActionHandler(mockPlayerService.Object, mockSpawnManager.Object);
        var character = new ServerCharacter { Id = 1 };
        var action = new TriggerAction
        {
            Type = "Spawn",
            Parameters = new Dictionary<string, string>
            {
                { "MonsterId", "123" },
                { "Count", "3" }
            }
        };

        handler.Execute(character, action);

        mockSpawnManager.Verify(s => s.StartScriptedEncounter(mockSession.Object, 123, 3), Times.Once);
    }

    [Fact]
    public void SetFlagAction_ShouldAddFlag()
    {
        var mockPlayerService = new Mock<PlayerService>(null, null);
        var qc = new PlayerQuestComponent(null, null);
        var session = new TestClientSession(qc);

        mockPlayerService.Setup(s => s.GetSession(It.IsAny<int>())).Returns(session);

        var handler = new SetFlagActionHandler(mockPlayerService.Object);
        var character = new ServerCharacter { Id = 1 };
        var action = new TriggerAction
        {
            Type = "SetFlag",
            Parameters = new Dictionary<string, string>
            {
                { "Flag", "TEST_FLAG" }
            }
        };

        handler.Execute(character, action);

        Assert.Contains("TEST_FLAG", qc.Flags);
    }

    private class TestClientSession : ClientSession
    {
        public TestClientSession(PlayerQuestComponent qc)
        {
            QuestComponent = qc;
        }
    }
}
