using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketIdempotencyTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketIdempotencyTests()
    {
        _economyMock = new Mock<IEconomyService>();
        _tradeMock = new Mock<TradeManager>();
        _dbMock = new Mock<IDbService>();
        
        var repoMock = new Mock<TWL.Server.Persistence.IPlayerRepository>();
        var metricsMock = new Mock<TWL.Server.Simulation.Managers.ServerMetrics>();
        _playerMock = new Mock<PlayerService>(repoMock.Object, metricsMock.Object);

        _marketManager = new MarketManager(_economyMock.Object, _tradeMock.Object, _playerMock.Object, _dbMock.Object);
    }

    [Fact]
    public async Task BuyListing_DuplicateOperationId_ReturnsSameResult()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 1000 };

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;
        var opId = Guid.NewGuid().ToString();

        var buyRequest = new BuyMarketListingRequest
        {
            ListingId = listingId,
            OperationId = opId
        };

        // Act
        var response1 = await _marketManager.BuyListingAsync(buyer, buyRequest);
        var response2 = await _marketManager.BuyListingAsync(buyer, buyRequest);

        // Assert
        Assert.True(response1.Success);
        Assert.True(response2.Success);
        Assert.Equal(response1.Message, response2.Message);
        Assert.Equal(response1.ListingId, response2.ListingId);
        Assert.Equal(response1.OperationId, response2.OperationId);
        
        // Buyer should only be charged ONCE
        Assert.Equal(500, buyer.Gold);
        
        // Seller should only be credited ONCE
        _playerMock.Verify(p => p.AddGoldAsync(1, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task BuyListing_DifferentOperationIds_ProcessMultiple()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 20);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 2000 };

        var createResponse1 = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        
        var createResponse2 = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });

        // Act
        var response1 = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = createResponse1.ListingId,
            OperationId = Guid.NewGuid().ToString()
        });
        
        var response2 = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = createResponse2.ListingId,
            OperationId = Guid.NewGuid().ToString()
        });

        // Assert
        Assert.True(response1.Success);
        Assert.True(response2.Success);
        Assert.Equal(1000, buyer.Gold); // 2000 - 500 - 500
        _playerMock.Verify(p => p.AddGoldAsync(1, It.IsAny<int>()), Times.Exactly(2));
    }
}
