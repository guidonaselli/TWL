using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketTaxCalculationTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketTaxCalculationTests()
    {
        _economyMock = new Mock<IEconomyService>();
        _tradeMock = new Mock<TradeManager>();
        _dbMock = new Mock<IDbService>();
        _economyMock.Setup(e => e.MarketTaxRate).Returns(0.05);
        
        var repoMock = new Mock<TWL.Server.Persistence.IPlayerRepository>();
        var metricsMock = new Mock<TWL.Server.Simulation.Managers.ServerMetrics>();
        _playerMock = new Mock<PlayerService>(repoMock.Object, metricsMock.Object);

        _marketManager = new MarketManager(_economyMock.Object, _tradeMock.Object, _playerMock.Object, _dbMock.Object);
    }

    [Theory]
    [InlineData(1000, 50, 950)]   // 1000 * 0.05 = 50
    [InlineData(100, 5, 95)]      // 100 * 0.05 = 5
    [InlineData(19, 0, 19)]       // 19 * 0.05 = 0.95 -> 0
    [InlineData(20, 1, 19)]       // 20 * 0.05 = 1.0 -> 1
    public async Task BuyListing_CalculatesTaxCorrectly(long totalPrice, long expectedTax, long expectedNet)
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 1);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = (int)totalPrice };
        // buyer.Gold = (int)totalPrice; // This line was causing CS8852 because Gold is init-only.

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 1,
            PricePerUnit = totalPrice
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = listingId
        });

        // Assert
        Assert.True(buyResponse.Success);
        
        // Check message for tax info
        Assert.Contains($"Tax applied: {expectedTax} Gold.", buyResponse.Message);
        
        // Verify seller received net amount
        _playerMock.Verify(p => p.AddGoldAsync(1, (int)expectedNet), Times.Once);
        
        // Verify DB record
        _dbMock.Verify(db => db.RecordMarketTransactionAsync(
            listingId, 2, 1, 101, "Item 101", 1, totalPrice, totalPrice, totalPrice, expectedTax, expectedNet), Times.Once);
    }

    [Fact]
    public async Task BuyListing_UsesConfigurableTaxRate()
    {
        // Arrange
        _economyMock.Setup(e => e.MarketTaxRate).Returns(0.10); // 10% tax
        
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 1);
        
        var buyer = new ServerCharacter { Id = 2, Name = "Buyer", Gold = 1000 };

        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 1,
            PricePerUnit = 1000
        });
        var listingId = createResponse.ListingId;

        // Act
        var buyResponse = await _marketManager.BuyListingAsync(buyer, new BuyMarketListingRequest
        {
            ListingId = listingId
        });

        // Assert
        Assert.True(buyResponse.Success);
        Assert.Contains("Tax applied: 100 Gold.", buyResponse.Message);
        _playerMock.Verify(p => p.AddGoldAsync(1, 900), Times.Once);
    }
}
