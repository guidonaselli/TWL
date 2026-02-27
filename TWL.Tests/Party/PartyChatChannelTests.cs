using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using Xunit;

namespace TWL.Tests.Party;

public class PartyChatChannelTests
{
    private readonly Mock<IPartyService> _partyServiceMock;
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly PartyChatService _chatService;
    private readonly Mock<ILogger<PartyChatService>> _loggerMock;

    public PartyChatChannelTests()
    {
        _partyServiceMock = new Mock<IPartyService>();
        // Mocking PlayerService is now possible because methods are virtual or we can use a subclass if needed.
        // Actually, PlayerService has virtual methods like GetSession, so we can mock it.
        // But the constructor requires arguments. We can pass nulls if we only use mocked virtual methods.
        _playerServiceMock = new Mock<PlayerService>(null, null);

        _loggerMock = new Mock<ILogger<PartyChatService>>();

        _chatService = new PartyChatService(
            _partyServiceMock.Object,
            _playerServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SendPartyMessage_ShouldBroadcast_ToAllMembers()
    {
        // Arrange
        var partyId = 100;
        var senderId = 1;
        var member2Id = 2;
        var content = "Hello Party";

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = partyId,
            LeaderId = senderId
        };
        party.MemberIds.Add(senderId);
        party.MemberIds.Add(member2Id);

        _partyServiceMock.Setup(s => s.GetParty(partyId)).Returns(party);

        // Mock sessions
        var session1 = new Mock<ClientSession>(); // ClientSession has protected ctor for testing
        var session2 = new Mock<ClientSession>();

        _playerServiceMock.Setup(ps => ps.GetSession(senderId)).Returns(session1.Object);
        _playerServiceMock.Setup(ps => ps.GetSession(member2Id)).Returns(session2.Object);

        // Act
        await _chatService.SendPartyMessageAsync(partyId, senderId, "Player1", content);

        // Assert
        // Verify session1 received message
        session1.Verify(s => s.SendAsync(It.Is<NetMessage>(m =>
            m.Op == Opcode.PartyChatBroadcast && m.JsonPayload.Contains(content))), Times.Once);

        // Verify session2 received message
        session2.Verify(s => s.SendAsync(It.Is<NetMessage>(m =>
            m.Op == Opcode.PartyChatBroadcast && m.JsonPayload.Contains(content))), Times.Once);
    }

    [Fact]
    public async Task SendPartyMessage_ShouldNotSend_IfSenderIsNotInParty()
    {
        // Arrange
        var partyId = 100;
        var senderId = 999; // Not in party
        var content = "Intruder Alert";

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = partyId,
            LeaderId = 1
        };
        party.MemberIds.Add(1);

        _partyServiceMock.Setup(s => s.GetParty(partyId)).Returns(party);

        // Act
        await _chatService.SendPartyMessageAsync(partyId, senderId, "Intruder", content);

        // Assert
        // Verify no interactions with PlayerService (optimization: if not in party, don't lookup sessions)
        _playerServiceMock.Verify(ps => ps.GetSession(It.IsAny<int>()), Times.Never);
    }
}
