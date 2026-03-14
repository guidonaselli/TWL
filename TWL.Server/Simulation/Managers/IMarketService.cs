using System.Collections.Generic;
using System.Threading.Tasks;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Domain service interface for the Peer-to-Peer Market System.
/// Handles all market-related logic and aggregate operations.
/// </summary>
public interface IMarketService
{
    /// <summary>
    /// Searches the market for active listings based on provided criteria.
    /// </summary>
    Task<MarketSearchResponse> SearchListingsAsync(MarketSearchRequest request);

    /// <summary>
    /// Retrieves all currently active market listings.
    /// </summary>
    IEnumerable<MarketListingDTO> GetActiveListings();

    /// <summary>
    /// Creates a new market listing for a character's item.
    /// </summary>
    Task<MarketOperationResponse> CreateListingAsync(ServerCharacter character, CreateMarketListingRequest request);

    /// <summary>
    /// Purchases an existing market listing.
    /// </summary>
    Task<MarketOperationResponse> BuyListingAsync(ServerCharacter buyer, BuyMarketListingRequest request);

    /// <summary>
    /// Cancels an active market listing.
    /// </summary>
    Task<MarketOperationResponse> CancelListingAsync(ServerCharacter seller, CancelMarketListingRequest request);

    /// <summary>
    /// Periodically processes expired market listings and returns items to sellers.
    /// </summary>
    Task ExpireListingsAsync();

    /// <summary>
    /// Projects market history (min/avg/max) for a specific item over a configurable time window.
    /// </summary>
    MarketHistoryDTO GetPriceHistory(int itemId, string window = "7d");
}
