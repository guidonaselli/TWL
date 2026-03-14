using System.Text.Json;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using TWL.Server.Persistence.Services;

namespace TWL.Tests.Market;

public class DirectTradeWindowTests
{
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly TradeSessionManager _tradeSessionManager;

    public DirectTradeWindowTests()
    {
        var repoMock = new Mock<TWL.Server.Persistence.IPlayerRepository>();
        var metricsMock = new Mock<TWL.Server.Simulation.Managers.ServerMetrics>();
        _playerMock = new Mock<PlayerService>(repoMock.Object, metricsMock.Object);
        _tradeMock = new Mock<TradeManager>();
        
        _tradeSessionManager = new TradeSessionManager(_playerMock.Object, _tradeMock.Object);
    }

    [Fact]
    public async Task TradeRequest_Sends_RequestToTarget()
    {
        // Arrange
        var inviterId = 1;
        var targetId = 2;
        
        var inviterSession = new Mock<ClientSession>();
        inviterSession.Setup(s => s.Character).Returns(new ServerCharacter { Id = 1, Name = "Inviter" });
        
        var targetSession = new Mock<ClientSession>();
        targetSession.Setup(s => s.Character).Returns(new ServerCharacter { Id = 2, Name = "Target" });

        _playerMock.Setup(p => p.GetSession(inviterId)).Returns(inviterSession.Object);
        _playerMock.Setup(p => p.GetSession(targetId)).Returns(targetSession.Object);

        // Act
        await _tradeSessionManager.RequestTradeAsync(inviterId, targetId);

        // Assert
        targetSession.Verify(s => s.SendAsync(It.Is<NetMessage>(m => m.Op == Opcode.TradeRequest)), Times.Once);
    }

    [Fact]
    public async Task AcceptTrade_StartsSession_AndBroadcastsState()
    {
        // Arrange
        var p1 = 1;
        var p2 = 2;
        
        var s1 = new Mock<ClientSession>();
        var s2 = new Mock<ClientSession>();

        _playerMock.Setup(p => p.GetSession(p1)).Returns(s1.Object);
        _playerMock.Setup(p => p.GetSession(p2)).Returns(s2.Object);

        // Act
        await _tradeSessionManager.AcceptTradeAsync(p2, p1);

        // Assert
        s1.Verify(s => s.SendAsync(It.Is<NetMessage>(m => m.Op == Opcode.TradeStateUpdate)), Times.Once);
        s2.Verify(s => s.SendAsync(It.Is<NetMessage>(m => m.Op == Opcode.TradeStateUpdate)), Times.Once);
    }

    [Fact]
    public async Task TradeConfirmation_BothParties_ExecutesTrade()
    {
        // Arrange
        var p1 = 1;
        var p2 = 2;
        
        var c1 = new ServerCharacter { Id = p1, Name = "P1", Gold = 1000 };
        var c2 = new ServerCharacter { Id = p2, Name = "P2", Gold = 1000 };

        var s1 = new Mock<ClientSession>();
        s1.Setup(s => s.Character).Returns(c1);
        var s2 = new Mock<ClientSession>();
        s2.Setup(s => s.Character).Returns(c2);

        _playerMock.Setup(p => p.GetSession(p1)).Returns(s1.Object);
        _playerMock.Setup(p => p.GetSession(p2)).Returns(s2.Object);

        await _tradeSessionManager.AcceptTradeAsync(p2, p1);
        
        // P1 offers 500 gold
        await _tradeSessionManager.UpdateOfferAsync(p1, new TradeOfferUpdateDto { Gold = 500 });

        // Act
        await _tradeSessionManager.ConfirmTradeAsync(p1);
        await _tradeSessionManager.ConfirmTradeAsync(p2);

        // Assert
        Assert.Equal(500, c1.Gold);
        Assert.Equal(1500, c2.Gold);
        s1.Verify(s => s.SendAsync(It.Is<NetMessage>(m => m.Op == Opcode.TradeComplete)), Times.Once);
        s2.Verify(s => s.SendAsync(It.Is<NetMessage>(m => m.Op == Opcode.TradeComplete)), Times.Once);
    }
}
