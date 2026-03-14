using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Service dedicated to searching and filtering market listings.
/// Separates query logic from the main MarketManager mutations.
/// </summary>
public class MarketQueryService
{
    private readonly IMarketService _marketService;

    public MarketQueryService(IMarketService marketService)
    {
        _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
    }

    /// <summary>
    /// Performs a filtered search on active market listings.
    /// </summary>
    public virtual Task<MarketSearchResponse> SearchAsync(MarketSearchRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var listings = _marketService.GetActiveListings();

        // Name Filter
        if (!string.IsNullOrEmpty(request.Query))
        {
            listings = listings.Where(l => l.ItemName.Contains(request.Query, StringComparison.OrdinalIgnoreCase));
        }

        // Type Filter
        if (request.Category.HasValue)
        {
            listings = listings.Where(l => l.ItemType == request.Category.Value);
        }

        // Rarity Filter
        if (request.Rarity.HasValue)
        {
            listings = listings.Where(l => l.Rarity == request.Rarity.Value);
        }

        // Price Range Filters
        if (request.MinPrice.HasValue)
        {
            listings = listings.Where(l => l.PricePerUnit >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            listings = listings.Where(l => l.PricePerUnit <= request.MaxPrice.Value);
        }

        var totalCount = listings.Count();
        
        // Sorting
        listings = request.SortBy switch
        {
            "PriceAsc" => listings.OrderBy(l => l.PricePerUnit),
            "PriceDesc" => listings.OrderByDescending(l => l.PricePerUnit),
            "Recent" => listings.OrderByDescending(l => l.CreatedUtc),
            _ => listings.OrderByDescending(l => l.CreatedUtc)
        };

        // Pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var results = listings
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new MarketSearchResponse
        {
            Listings = results,
            TotalCount = totalCount,
            Page = page,
            TotalPages = totalPages
        });
    }
}
