using System;
using System.Collections.Generic;
using TWL.Shared.Domain.Models;

namespace TWL.Shared.Domain.DTO;

/// <summary>
/// Status of a market listing.
/// </summary>
public enum MarketListingStatus
{
    Active,
    Cancelled,
    Expired,
    Sold
}

/// <summary>
/// Summary of a market listing for browsing and purchasing.
/// </summary>
public class MarketListingDTO
{
    public string ListingId { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Quantity { get; set; }
    public long PricePerUnit { get; set; }
    public long TotalPrice { get; set; }
    public MarketListingStatus Status { get; set; } = MarketListingStatus.Active;
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

/// <summary>
/// Request to search for market listings with filters.
/// </summary>
public class MarketSearchRequest
{
    public string? Query { get; set; }
    public ItemType? Category { get; set; }
    public ItemRarity? Rarity { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public string? SortBy { get; set; } // e.g. "PriceAsc", "PriceDesc", "Recent"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response containing market search results.
/// </summary>
public class MarketSearchResponse
{
    public List<MarketListingDTO> Listings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Request to create a new market listing.
/// </summary>
public class CreateMarketListingRequest
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public long PricePerUnit { get; set; }
    public string OperationId { get; set; } = string.Empty;
}

/// <summary>
/// Request to buy an existing market listing.
/// </summary>
public class BuyMarketListingRequest
{
    public string ListingId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
}

/// <summary>
/// Request to cancel a market listing.
/// </summary>
public class CancelMarketListingRequest
{
    public string ListingId { get; set; } = string.Empty;
}

/// <summary>
/// Generic response for market operations (Create, Buy, Cancel).
/// </summary>
public class MarketOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ListingId { get; set; }
    public string? OperationId { get; set; }
    public long? NewBalance { get; set; }
}

/// <summary>
/// Projection of price history for an item.
/// </summary>
public class MarketHistoryDTO
{
    public int ItemId { get; set; }
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public long AveragePrice { get; set; }
    public int SampleCount { get; set; }
    public long Volume { get; set; }
    public string Window { get; set; } = string.Empty; // "24h", "7d", "30d"
}

/// <summary>
/// Aggregate statistics for the marketplace.
/// </summary>
public class MarketStatsDTO
{
    public int ActiveListingCount { get; set; }
    public long TotalGoldVolume { get; set; }
    public long TotalTaxCollected { get; set; }
    public int TotalItemsSold { get; set; }
}
