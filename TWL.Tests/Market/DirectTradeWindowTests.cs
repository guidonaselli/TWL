using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Shared.Net.Network;
using Xunit;

namespace TWL.Tests.Market;

public class DirectTradeWindowTests
{
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly TradeManager _tradeManager;
    private readonly TradeSessionManager _sessionManager;

    public DirectTradeWindowTests()
    {
        _playerServiceMock = new Mock<PlayerService>(null, null);
        _tradeManager = new TradeManager();
        _sessionManager = new TradeSessionManager(_playerServiceMock.Object, _tradeManager);
    }

    [Fact]
    public async Task Trade_SuccessfulExchange_MovesItemsAndGold()
    {
        // Arrange
        var p1Id = 1;
        var p2Id = 2;

        var p1 = new ServerCharacter { Id = p1Id, Name = "P1", Gold = 1000 };
        var p2 = new ServerCharacter { Id = p2Id, Name = "P2", Gold = 1000 };

        p1.AddItem(10, 5); // 5x Item 10
        p2.AddItem(20, 3); // 3x Item 20

        var s1Mock = new Mock<ClientSession>();
        s1Mock.SetupGet(s => s.Character).Returns(p1);
        s1Mock.SetupGet(s => s.UserId).Returns(p1Id);

        var s2Mock = new Mock<ClientSession>();
        s2Mock.SetupGet(s => s.Character).Returns(p2);
        s2Mock.SetupGet(s => s.UserId).Returns(p2Id);

        _playerServiceMock.Setup(s => s.GetSession(p1Id)).Returns(s1Mock.Object);
        _playerServiceMock.Setup(s => s.GetSession(p2Id)).Returns(s2Mock.Object);

        // Act
        await _sessionManager.AcceptTradeAsync(p2Id, p1Id);
        
        // P1 offers 2x Item 10 and 100 Gold
        await _sessionManager.UpdateOfferAsync(p1Id, new TradeOfferUpdateDto
        {
            Gold = 100,
            Items = new List<TradeItemDto> { new() { ItemId = 10, Quantity = 2 } }
        });

        // P2 offers 1x Item 20 and 50 Gold
        await _sessionManager.UpdateOfferAsync(p2Id, new TradeOfferUpdateDto
        {
            Gold = 50,
            Items = new List<TradeItemDto> { new() { ItemId = 20, Quantity = 1 } }
        });

        // Both confirm
        await _sessionManager.ConfirmTradeAsync(p1Id);
        await _sessionManager.ConfirmTradeAsync(p2Id);

        // Assert
        Assert.Equal(1000 - 100 + 50, p1.Gold);
        Assert.Equal(1000 - 50 + 100, p2.Gold);

        Assert.Equal(3, p1.GetItems(10).Sum(i => i.Quantity));
        Assert.Equal(2, p2.GetItems(10).Sum(i => i.Quantity));

        Assert.Equal(1, p1.GetItems(20).Sum(i => i.Quantity));
        Assert.Equal(2, p2.GetItems(20).Sum(i => i.Quantity));
    }

    [Fact]
    public async Task Trade_BindPolicyViolation_RejectsTransfer()
    {
        // Arrange
        var p1Id = 1;
        var p2Id = 2;

        var p1 = new ServerCharacter { Id = p1Id, Name = "P1" };
        var p2 = new ServerCharacter { Id = p2Id, Name = "P2" };

        // P1 has a CharacterBound item
        p1.AddItem(99, 1, BindPolicy.CharacterBound, p1Id);

        var s1Mock = new Mock<ClientSession>();
        s1Mock.SetupGet(s => s.Character).Returns(p1);
        _playerServiceMock.Setup(s => s.GetSession(p1Id)).Returns(s1Mock.Object);

        var s2Mock = new Mock<ClientSession>();
        s2Mock.SetupGet(s => s.Character).Returns(p2);
        _playerServiceMock.Setup(s => s.GetSession(p2Id)).Returns(s2Mock.Object);

        await _sessionManager.AcceptTradeAsync(p2Id, p1Id);

        // Act
        await _sessionManager.UpdateOfferAsync(p1Id, new TradeOfferUpdateDto
        {
            Items = new List<TradeItemDto> { new() { ItemId = 99, Quantity = 1 } }
        });

        await _sessionManager.ConfirmTradeAsync(p1Id);
        await _sessionManager.ConfirmTradeAsync(p2Id);

        // Assert
        Assert.Equal(1, p1.GetItems(99).Sum(i => i.Quantity));
        Assert.Empty(p2.GetItems(99));
    }
}
