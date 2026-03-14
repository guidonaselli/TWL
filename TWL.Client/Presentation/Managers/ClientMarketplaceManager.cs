// File: `TWL.Client/Managers/ClientMarketplaceManager.cs`

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Managers;

/// <summary>
/// Client-side manager for the Peer-to-Peer Marketplace.
/// Responsible for maintaining local state of listings and history received from the server.
/// </summary>
public class ClientMarketplaceManager
{
    private readonly ConcurrentDictionary<string, MarketListingDTO> _activeListings = new();
    private readonly ConcurrentDictionary<int, MarketHistoryDTO> _priceHistory = new();

    /// <summary>
    /// Gets the current set of active market listings locally cached.
    /// </summary>
    public IEnumerable<MarketListingDTO> ActiveListings => _activeListings.Values;

    /// <summary>
    /// Event triggered when the marketplace state is updated.
    /// </summary>
    public event Action? OnMarketUpdated;

    /// <summary>
    /// Processes a marketplace state update from the server.
    /// Wires listing snapshots and price-history payloads into the client state.
    /// </summary>
    /// <param name="update">The update containing listing snapshots and history payloads.</param>
    public void OnMarketplaceUpdate(MarketplaceUpdate update)
    {
        if (update == null) return;

        // Rebuild Active Listings from the server snapshot
        if (update.ItemsForSale != null)
        {
            _activeListings.Clear();
            foreach (var listing in update.ItemsForSale)
            {
                _activeListings[listing.ListingId] = listing;
            }
        }
        
        // Update History
        if (update.History != null)
        {
            foreach (var history in update.History)
            {
                _priceHistory[history.ItemId] = history;
            }
        }

        OnMarketUpdated?.Invoke();
    }

    /// <summary>
    /// Returns the cached price history for a specific item.
    /// </summary>
    public MarketHistoryDTO? GetHistory(int itemId)
    {
        return _priceHistory.TryGetValue(itemId, out var history) ? history : null;
    }

    /// <summary>
    /// Updates the local listings cache with a search response.
    /// </summary>
    public void UpdateListingsFromSearch(MarketSearchResponse response)
    {
        if (response?.Listings == null) return;

        _activeListings.Clear();
        foreach (var listing in response.Listings)
        {
            _activeListings[listing.ListingId] = listing;
        }

        OnMarketUpdated?.Invoke();
    }
}