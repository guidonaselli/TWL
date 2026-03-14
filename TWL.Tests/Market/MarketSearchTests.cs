using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;

namespace TWL.Tests.Market;

public class MarketSearchTests
{
    private readonly Mock<IMarketService> _marketServiceMock;
    private readonly MarketQueryService _queryService;

    public MarketSearchTests()
    {
        _marketServiceMock = new Mock<IMarketService>();
        _queryService = new MarketQueryService(_marketServiceMock.Object);
    }

    private List<MarketListingDTO> GetMockListings()
    {
        return new List<MarketListingDTO>
        {
            new MarketListingDTO { ListingId = "1", ItemName = "Iron Sword", ItemType = ItemType.Equipment, Rarity = ItemRarity.Common, PricePerUnit = 100, CreatedUtc = DateTime.UtcNow.AddMinutes(-10) },
            new MarketListingDTO { ListingId = "2", ItemName = "Steel Sword", ItemType = ItemType.Equipment, Rarity = ItemRarity.Uncommon, PricePerUnit = 500, CreatedUtc = DateTime.UtcNow.AddMinutes(-5) },
            new MarketListingDTO { ListingId = "3", ItemName = "Health Potion", ItemType = ItemType.Consumable, Rarity = ItemRarity.Common, PricePerUnit = 50, CreatedUtc = DateTime.UtcNow.AddMinutes(-20) },
            new MarketListingDTO { ListingId = "4", ItemName = "Mana Potion", ItemType = ItemType.Consumable, Rarity = ItemRarity.Common, PricePerUnit = 60, CreatedUtc = DateTime.UtcNow.AddMinutes(-15) },
            new MarketListingDTO { ListingId = "5", ItemName = "Golden Armor", ItemType = ItemType.Equipment, Rarity = ItemRarity.Rare, PricePerUnit = 5000, CreatedUtc = DateTime.UtcNow.AddMinutes(-1) }
        };
    }

    [Fact]
    public async Task Search_ByName_ReturnsCorrectListings()
    {
        // Arrange
        _marketServiceMock.Setup(m => m.GetActiveListings()).Returns(GetMockListings());
        var request = new MarketSearchRequest { Query = "Sword" };

        // Act
        var response = await _queryService.SearchAsync(request);

        // Assert
        Assert.Equal(2, response.TotalCount);
        Assert.All(response.Listings, l => Assert.Contains("Sword", l.ItemName));
    }

    [Fact]
    public async Task Search_ByCategory_ReturnsCorrectListings()
    {
        // Arrange
        _marketServiceMock.Setup(m => m.GetActiveListings()).Returns(GetMockListings());
        var request = new MarketSearchRequest { Category = ItemType.Consumable };

        // Act
        var response = await _queryService.SearchAsync(request);

        // Assert
        Assert.Equal(2, response.TotalCount);
        Assert.All(response.Listings, l => Assert.Equal(ItemType.Consumable, l.ItemType));
    }

    [Fact]
    public async Task Search_ByPriceRange_ReturnsCorrectListings()
    {
        // Arrange
        _marketServiceMock.Setup(m => m.GetActiveListings()).Returns(GetMockListings());
        var request = new MarketSearchRequest { MinPrice = 60, MaxPrice = 500 };

        // Act
        var response = await _queryService.SearchAsync(request);

        // Assert
        // Should return Iron Sword (100), Steel Sword (500), Mana Potion (60)
        Assert.Equal(3, response.TotalCount);
        Assert.All(response.Listings, l => Assert.True(l.PricePerUnit >= 60 && l.PricePerUnit <= 500));
    }

    [Fact]
    public async Task Search_SortByPriceAsc_ReturnsSortedListings()
    {
        // Arrange
        _marketServiceMock.Setup(m => m.GetActiveListings()).Returns(GetMockListings());
        var request = new MarketSearchRequest { SortBy = "PriceAsc" };

        // Act
        var response = await _queryService.SearchAsync(request);

        // Assert
        var prices = response.Listings.Select(l => l.PricePerUnit).ToList();
        var sortedPrices = prices.OrderBy(p => p).ToList();
        Assert.Equal(sortedPrices, prices);
    }

    [Fact]
    public async Task Search_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        _marketServiceMock.Setup(m => m.GetActiveListings()).Returns(GetMockListings());
        var request = new MarketSearchRequest { PageSize = 2, Page = 2 };

        // Act
        var response = await _queryService.SearchAsync(request);

        // Assert
        Assert.Equal(2, response.Listings.Count);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(2, response.Page);
    }
}