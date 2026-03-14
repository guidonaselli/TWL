using System.Collections.Concurrent;
using System.Reflection;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketExpirationTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketExpirationTests()
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
    public async Task ExpireListings_ReturnsItemsToSeller()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Force expiration via reflection
        SetListingExpiration(listingId, DateTime.UtcNow.AddHours(-1));

        _playerMock.Setup(p => p.ReturnMarketItemAsync(1, 101, 5))
            .ReturnsAsync(true);

        // Act
        await _marketManager.ExpireListingsAsync();

        // Assert
        _playerMock.Verify(p => p.ReturnMarketItemAsync(1, 101, 5), Times.Once);
        
        var searchResponse = await _marketManager.SearchListingsAsync(new MarketSearchRequest { Query = "Item 101" });
        Assert.Empty(searchResponse.Listings); // Filtered out because it's Expired
    }

    [Fact]
    public async Task ExpireListings_Twice_DoesNotDoubleReturnItems()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        SetListingExpiration(listingId, DateTime.UtcNow.AddHours(-1));

        _playerMock.Setup(p => p.ReturnMarketItemAsync(1, 101, 5))
            .ReturnsAsync(true);

        // Act
        await _marketManager.ExpireListingsAsync();
        await _marketManager.ExpireListingsAsync(); // Second call

        // Assert
        _playerMock.Verify(p => p.ReturnMarketItemAsync(1, 101, 5), Times.Once); // Only ONCE
    }

    private void SetListingExpiration(string listingId, DateTime expiresUtc)
    {
        var field = typeof(MarketManager).GetField("_listings", BindingFlags.NonPublic | BindingFlags.Instance);
        var listings = (ConcurrentDictionary<string, MarketListingDTO>)field.GetValue(_marketManager);
        if (listings.TryGetValue(listingId, out var listing))
        {
            listing.ExpiresUtc = expiresUtc;
        }
    }
}
