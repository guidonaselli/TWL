using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketPurchaseSettlementTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketPurchaseSettlementTests()
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
    public async Task BuyListing_Success_SettlesCorrectly()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 1000 };
        // buyer.Gold = 1000; // Gold is init-only.

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = listingId
        });

        // Assert
        Assert.True(buyResponse.Success);
        Assert.Equal(500, buyer.Gold); // 1000 - (5 * 100)
        Assert.True(buyer.HasItem(101, 5));
        
        // Seller settlement (500 gross - 5% tax = 475 net)
        _playerMock.Verify(p => p.AddGoldAsync(1, 475), Times.Once);
        
        // Database record
        _dbMock.Verify(db => db.RecordMarketTransactionAsync(
            listingId, 2, 1, 101, "Item 101", 5, 100, 500, 500, 25, 475), Times.Once);
    }

    [Fact]
    public async Task BuyListing_InsufficientFunds_Fails()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 100 }; // Not enough for 500 total
        // buyer.Gold = 100; // Gold is init-only.

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = listingId
        });

        // Assert
        Assert.False(buyResponse.Success);
        Assert.Equal("Insufficient gold.", buyResponse.Message);
        Assert.Equal(100, buyer.Gold);
        Assert.False(buyer.HasItem(101, 5));
    }

    [Fact]
    public async Task BuyListing_AlreadySold_Fails()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var buyer1 = new ServerCharacter { Id = 2, Name = "Buyer1", Gold = 1000 };
        // buyer1.Gold = 1000;
        var buyer2 = new ServerCharacter { Id = 3, Name = "Buyer2", Gold = 1000 };
        // buyer2.Gold = 1000;

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse1 = await _marketManager.BuyListingAsync(buyer1, new BuyMarketListingRequest { ListingId = listingId });
        var buyResponse2 = await _marketManager.BuyListingAsync(buyer2, new BuyMarketListingRequest { ListingId = listingId });

        // Assert
        Assert.True(buyResponse1.Success);
        Assert.False(buyResponse2.Success);
        Assert.Equal("Listing is no longer active.", buyResponse2.Message);
    }

    [Fact]
    public async Task BuyListing_OwnListing_Fails()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller", Gold = 1000 };
        // seller.Gold = 1000;
        seller.AddItem(101, 10);

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse = await _marketManager.BuyListingAsync(seller, new BuyMarketListingRequest
        {
            ListingId = listingId
        });

        // Assert
        Assert.False(buyResponse.Success);
        Assert.Equal("You cannot buy your own listing.", buyResponse.Message);
    }
}
