using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketListingLifecycleTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly Mock<TradeManager> _tradeMock;
    private readonly Mock<PlayerService> _playerMock;
    private readonly Mock<IDbService> _dbMock;
    private readonly MarketManager _marketManager;

    public MarketListingLifecycleTests()
    {
        _economyMock = new Mock<IEconomyService>();
        _tradeMock = new Mock<TradeManager>();
        _dbMock = new Mock<IDbService>();
        
        // Mock PlayerService dependencies if needed, but we can just mock the service itself if it's virtual
        var repoMock = new Mock<TWL.Server.Persistence.IPlayerRepository>();
        var metricsMock = new Mock<TWL.Server.Simulation.Managers.ServerMetrics>();
        _playerMock = new Mock<PlayerService>(repoMock.Object, metricsMock.Object);

        _marketManager = new MarketManager(_economyMock.Object, _tradeMock.Object, _playerMock.Object, _dbMock.Object);
    }

    [Fact]
    public async Task CreateListing_Success_RemovesItemFromInventory()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10); // Item 101, Qty 10
        
        var request = new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100,
            OperationId = "op-1"
        };

        // Act
        var response = await _marketManager.CreateListingAsync(seller, request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(5, seller.Inventory.First(i => i.ItemId == 101).Quantity);
        
        var searchResponse = await _marketManager.SearchListingsAsync(new MarketSearchRequest { Query = "Item 101" });
        Assert.Single(searchResponse.Listings);
        Assert.Equal(MarketListingStatus.Active, searchResponse.Listings[0].Status);
    }

    [Fact]
    public async Task CreateListing_InsufficientItems_ReturnsFailure()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 3); // Only 3 items
        
        var request = new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5, // Want 5
            PricePerUnit = 100,
            OperationId = "op-2"
        };

        // Act
        var response = await _marketManager.CreateListingAsync(seller, request);

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Item not found in inventory.", response.Message);
        Assert.Equal(3, seller.Inventory.First(i => i.ItemId == 101).Quantity);
    }

    [Fact]
    public async Task CancelListing_Owner_ReturnsItemsToInventory()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        seller.AddItem(101, 10);
        
        var createRequest = new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        };
        var createResponse = await _marketManager.CreateListingAsync(seller, createRequest);
        var listingId = createResponse.ListingId;

        var cancelRequest = new CancelMarketListingRequest { ListingId = listingId };

        // Act
        var cancelResponse = await _marketManager.CancelListingAsync(seller, cancelRequest);

        // Assert
        Assert.True(cancelResponse.Success);
        Assert.Equal(10, seller.Inventory.First(i => i.ItemId == 101).Quantity);
        
        var searchResponse = await _marketManager.SearchListingsAsync(new MarketSearchRequest { Query = "Item 101" });
        Assert.Empty(searchResponse.Listings); // Filtered out because it's Cancelled
    }

    [Fact]
    public async Task CancelListing_NonOwner_ReturnsUnauthorized()
    {
        // Arrange
        var seller = new ServerCharacter { Id = 1, Name = "Seller" };
        var thief = new ServerCharacter { Id = 2, Name = "Thief" };
        seller.AddItem(101, 10);
        
        var createResponse = await _marketManager.CreateListingAsync(seller, new CreateMarketListingRequest
        {
            ItemId = 101,
            Quantity = 5,
            PricePerUnit = 100
        });
        var listingId = createResponse.ListingId;

        // Act
        var cancelResponse = await _marketManager.CancelListingAsync(thief, new CancelMarketListingRequest { ListingId = listingId });

        // Assert
        Assert.False(cancelResponse.Success);
        Assert.Equal("Unauthorized cancellation attempt.", cancelResponse.Message);
        Assert.Equal(5, seller.Inventory.First(i => i.ItemId == 101).Quantity); // Seller still minus items
    }

    [Fact]
    public async Task CancelListing_Twice_DoesNotDoubleReturnItems()
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

        // Act
        await _marketManager.CancelListingAsync(seller, new CancelMarketListingRequest { ListingId = listingId });
        var secondCancelResponse = await _marketManager.CancelListingAsync(seller, new CancelMarketListingRequest { ListingId = listingId });

        // Assert
        Assert.False(secondCancelResponse.Success);
        Assert.Equal("Listing is no longer active.", secondCancelResponse.Message);
        Assert.Equal(10, seller.Inventory.First(i => i.ItemId == 101).Quantity); // Should NOT be 15
    }
}
