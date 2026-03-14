using Moq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketPriceHistoryTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketPriceHistoryTests()
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
    public async Task GetPriceHistory_CalculatesMathCorrectly()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller", Gold = 0 };
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 10000 };
        seller.AddItem(101, 100);

        // Record some sales by actually performing buy operations
        // Sale 1: 10 units @ 100 gold
        var createResponse1 = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest { ItemId = 101, Quantity = 10, PricePerUnit = 100 });
        await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest { ListingId = createResponse1.ListingId });

        // Sale 2: 5 units @ 200 gold
        var createResponse2 = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest { ItemId = 101, Quantity = 5, PricePerUnit = 200 });
        await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest { ListingId = createResponse2.ListingId });

        // Sale 3: 20 units @ 150 gold
        var createResponse3 = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest { ItemId = 101, Quantity = 20, PricePerUnit = 150 });
        await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest { ListingId = createResponse3.ListingId });

        // Act
        var history = _marketManager.GetPriceHistory(101);

        // Assert
        Assert.Equal(101, history.ItemId);
        Assert.Equal(100, history.MinPrice);
        Assert.Equal(200, history.MaxPrice);
        Assert.Equal(150, history.AveragePrice); // (100 + 200 + 150) / 3 = 150
        Assert.Equal(3, history.SampleCount);
        Assert.Equal(35, history.Volume); // 10 + 5 + 20 = 35
    }

    [Fact]
    public void GetPriceHistory_EmptyHistory_ReturnsZeros()
    {
        // Act
        var history = _marketManager.GetPriceHistory(999);

        // Assert
        Assert.Equal(999, history.ItemId);
        Assert.Equal(0, history.MinPrice);
        Assert.Equal(0, history.MaxPrice);
        Assert.Equal(0, history.AveragePrice);
        Assert.Equal(0, history.SampleCount);
        Assert.Equal(0, history.Volume);
    }

    [Fact]
    public async Task GetPriceHistory_FiltersByTimeWindow()
    {
        // Arrange
        // Note: MarketManager uses DateTime.UtcNow for RecordSaleHistory.
        // We can't easily inject time into MarketManager currently as it uses DateTime.UtcNow directly.
        // However, we can test that the "7d" window is the default and it works for recent sales.
        
        var seller = new ServerCharacter { Id = 1, Name = "Seller", Gold = 0 };
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 10000 };
        seller.AddItem(101, 10);
        
        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest { ItemId = 101, Quantity = 1, PricePerUnit = 100 });
        await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest { ListingId = createResponse.ListingId });

        // Act
        var historyRecent = _marketManager.GetPriceHistory(101, "24h");
        var historyNone = _marketManager.GetPriceHistory(101, "invalid_window_defaults_to_7d");

        // Assert
        Assert.Equal(1, historyRecent.SampleCount);
        Assert.Equal(1, historyNone.SampleCount);
    }
}