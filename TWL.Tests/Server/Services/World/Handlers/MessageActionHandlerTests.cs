using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World.Actions;
using TWL.Server.Services.World.Actions.Handlers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;
using System.Text.Json;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;

namespace TWL.Tests.Server.Services.World.Handlers;

public class MessageActionHandlerTests
{
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly MessageActionHandler _handler;

    public MessageActionHandlerTests()
    {
        var metrics = new ServerMetrics();
        var repo = new Mock<IPlayerRepository>();
        _playerServiceMock = new Mock<PlayerService>(repo.Object, metrics);
        _handler = new MessageActionHandler(_playerServiceMock.Object);
    }

    [Fact]
    public void Execute_SendsSystemMessage_WhenSessionExists()
    {
        var character = new ServerCharacter { Id = 123, Name = "TestPlayer" };
        var triggerAction = new TriggerAction
        {
            Type = "Message",
            Parameters = new Dictionary<string, string>
            {
                { "Text", "Welcome to the server!" }
            }
        };

        var sessionMock = new Mock<ClientSession>();
        _playerServiceMock.Setup(s => s.GetSession(123)).Returns(sessionMock.Object);

        _handler.Execute(character, triggerAction);

        sessionMock.Verify(s => s.SendAsync(It.Is<NetMessage>(msg =>
            msg.Op == Opcode.SystemMessage &&
            JsonSerializer.Deserialize<SystemMessageDto>(msg.JsonPayload, (JsonSerializerOptions?)null).Text == "Welcome to the server!"
        )), Times.Once);
    }

    [Fact]
    public void Execute_DoesNothing_WhenSessionDoesNotExist()
    {
        var character = new ServerCharacter { Id = 456, Name = "OfflinePlayer" };
        var triggerAction = new TriggerAction
        {
            Type = "Message",
            Parameters = new Dictionary<string, string>
            {
                { "Text", "Hello?" }
            }
        };

        _playerServiceMock.Setup(s => s.GetSession(456)).Returns((ClientSession)null);

        // No exception should be thrown
        _handler.Execute(character, triggerAction);
    }
}
