using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;
using TWL.Server.Security;
using TWL.Server.Security.Idempotency;
using TWL.Server.Persistence.Services;
using TWL.Server.Persistence.Database;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Implementation of the Peer-to-Peer Market System manager.
/// Manages market listings, searches, and player transactions.
/// </summary>
public class MarketManager : IMarketService
{
    private const int IDEMPOTENCY_EXPIRATION_MINUTES = 10;

    private readonly ConcurrentDictionary<string, MarketListingDTO> _listings = new();
    private readonly ConcurrentBag<SaleRecord> _saleHistory = new();
    private readonly IEconomyService _economyService;
    private readonly TradeManager _tradeManager;
    private readonly PlayerService _playerService;
    private readonly IDbService _dbService;
    private readonly IdempotencyValidator _idempotencyValidator = new(TimeSpan.FromMinutes(IDEMPOTENCY_EXPIRATION_MINUTES));

    private class SaleRecord
    {
        public int ItemId { get; set; }
        public long PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public long GrossAmount { get; set; }
        public long TaxAmount { get; set; }
        public long NetAmount { get; set; }
        public int SellerId { get; set; }
        public int BuyerId { get; set; }
        public DateTime SoldUtc { get; set; }
    }

    public MarketManager(IEconomyService economyService, TradeManager tradeManager, PlayerService playerService, IDbService dbService)
    {
        ArgumentNullException.ThrowIfNull(economyService);
        ArgumentNullException.ThrowIfNull(tradeManager);
        ArgumentNullException.ThrowIfNull(playerService);
        ArgumentNullException.ThrowIfNull(dbService);
        _economyService = economyService;
        _tradeManager = tradeManager;
        _playerService = playerService;
        _dbService = dbService;
    }

    public Task<MarketSearchResponse> SearchListingsAsync(MarketSearchRequest request)
    {
        // Filter only Active listings
        var query = _listings.Values.Where(l => l.Status == MarketListingStatus.Active);

        if (!string.IsNullOrEmpty(request.Query))
        {
            query = query.Where(l => l.ItemName.Contains(request.Query, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Category.HasValue)
        {
            // Category filtering would require joining with item metadata
        }

        var totalCount = query.Count();
        var pageCount = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var listings = query
            .OrderByDescending(l => l.CreatedUtc) // Default sort
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new MarketSearchResponse
        {
            Listings = listings,
            TotalCount = totalCount,
            Page = request.Page,
            TotalPages = pageCount
        });
    }

    public IEnumerable<MarketListingDTO> GetActiveListings()
    {
        return _listings.Values.Where(l => l.Status == MarketListingStatus.Active);
    }

    public async Task<MarketOperationResponse> CreateListingAsync(ServerCharacter character, CreateMarketListingRequest request)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(request);

        // Enforce valid bounds
        if (request.Quantity <= 0 || request.Quantity > 999)
        {
            return new MarketOperationResponse { Success = false, Message = "Invalid quantity (1-999)." };
        }

        if (request.PricePerUnit <= 0 || request.PricePerUnit > 99_999_999)
        {
            return new MarketOperationResponse { Success = false, Message = "Invalid price." };
        }

        // Check inventory availability
        if (!character.HasItem(request.ItemId, request.Quantity))
        {
            return new MarketOperationResponse { Success = false, Message = "Item not found in inventory." };
        }

        // Inventory is locked during the listing (removed from character)
        if (!character.RemoveItem(request.ItemId, request.Quantity))
        {
            return new MarketOperationResponse { Success = false, Message = "Failed to lock items for listing." };
        }

        var listing = new MarketListingDTO
        {
            ListingId = Guid.NewGuid().ToString(),
            SellerId = character.Id,
            SellerName = character.Name,
            ItemId = request.ItemId,
            ItemName = $"Item {request.ItemId}", // In a real system we'd get the name from a database
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            TotalPrice = (long)request.Quantity * request.PricePerUnit,
            Status = MarketListingStatus.Active,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(7) // 7 days expiration
        };

        if (_listings.TryAdd(listing.ListingId, listing))
        {
            // Persist to database
            await _dbService.CreateMarketListingAsync(listing.ListingId, listing.SellerId, listing.ItemId, listing.ItemName, listing.Quantity, listing.PricePerUnit, listing.TotalPrice, listing.ExpiresUtc);

            SecurityLogger.LogSecurityEvent("MarketListingCreated", character.Id, $"ListingId:{listing.ListingId} ItemId:{listing.ItemId} Qty:{listing.Quantity} TotalPrice:{listing.TotalPrice}");
            return new MarketOperationResponse 
            { 
                Success = true, 
                ListingId = listing.ListingId,
                OperationId = request.OperationId,
                Message = "Listing created successfully."
            };
        }

        // Rollback if TryAdd fails (unlikely)
        character.AddItem(request.ItemId, request.Quantity);
        return new MarketOperationResponse { Success = false, Message = "System error creating listing." };
    }

    public async Task<MarketOperationResponse> BuyListingAsync(ServerCharacter buyer, BuyMarketListingRequest request)
    {
        ArgumentNullException.ThrowIfNull(buyer);
        ArgumentNullException.ThrowIfNull(request);

        // Idempotency Check
        bool hasOperation = !string.IsNullOrEmpty(request.OperationId);
        if (hasOperation)
        {
            if (!_idempotencyValidator.TryRegisterOperation(request.OperationId, buyer.Id, out var existingRecord))
            {
                lock (existingRecord)
                {
                    if (existingRecord.State == OperationState.Completed)
                    {
                        return (MarketOperationResponse)existingRecord.SemanticResult!;
                    }

                    if (existingRecord.State == OperationState.Pending)
                    {
                        return new MarketOperationResponse { Success = false, Message = "Transaction in progress." };
                    }
                    // If Failed, allow retry by setting back to pending
                    existingRecord.State = OperationState.Pending;
                }
            }
        }

        if (!_listings.TryGetValue(request.ListingId, out var listing))
        {
            if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
            return new MarketOperationResponse { Success = false, Message = "Listing not found." };
        }

        // restricted to buyer cannot be the seller
        if (listing.SellerId == buyer.Id)
        {
            if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
            return new MarketOperationResponse { Success = false, Message = "You cannot buy your own listing." };
        }

        // Terminal states check
        if (listing.Status != MarketListingStatus.Active)
        {
            if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
            return new MarketOperationResponse { Success = false, Message = "Listing is no longer active." };
        }

        // Currency check
        if (buyer.Gold < listing.TotalPrice)
        {
            if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
            return new MarketOperationResponse { Success = false, Message = "Insufficient gold." };
        }

        long grossAmount = listing.TotalPrice;
        long taxAmount = (long)(grossAmount * _economyService.MarketTaxRate);
        long netAmount = grossAmount - taxAmount;

        // Atomic transition attempt
        lock (listing)
        {
            if (listing.Status != MarketListingStatus.Active)
            {
                if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
                return new MarketOperationResponse { Success = false, Message = "Listing was just purchased or expired." };
            }

            // Deduct gold from buyer
            if (!buyer.TryConsumeGold((int)listing.TotalPrice))
            {
                if (hasOperation) _idempotencyValidator.MarkFailed(request.OperationId);
                return new MarketOperationResponse { Success = false, Message = "Failed to process payment." };
            }

            listing.Status = MarketListingStatus.Sold;
        }

        // Update persistence (moved outside of lock to avoid await-in-lock)
        await _dbService.UpdateMarketListingStatusAsync(listing.ListingId, false);

        // Grant items to buyer
        buyer.AddItem(listing.ItemId, listing.Quantity);

        // Send gold to seller (handles offline cases via PlayerService)
        // Seller receives net amount (gross - tax)
        await _playerService.AddGoldAsync(listing.SellerId, (int)netAmount);

        // Record for price history projection and audit
        await RecordSaleHistory(listing.ListingId, listing.ItemId, listing.ItemName, listing.PricePerUnit, listing.Quantity, listing.TotalPrice, grossAmount, taxAmount, netAmount, listing.SellerId, buyer.Id);

        SecurityLogger.LogSecurityEvent("MarketPurchase", buyer.Id, $"ListingId:{listing.ListingId} SellerId:{listing.SellerId} ItemId:{listing.ItemId} Qty:{listing.Quantity} Gross:{grossAmount} Tax:{taxAmount} Net:{netAmount}");

        var response = new MarketOperationResponse
        {
            Success = true,
            ListingId = listing.ListingId,
            OperationId = request.OperationId,
            NewBalance = (long)buyer.Gold,
            Message = $"Purchase completed successfully. Tax applied: {taxAmount} Gold."
        };

        if (hasOperation)
        {
            _idempotencyValidator.MarkCompleted(request.OperationId, response);
        }

        return response;
    }

    public async Task<MarketOperationResponse> CancelListingAsync(ServerCharacter seller, CancelMarketListingRequest request)
    {
        ArgumentNullException.ThrowIfNull(seller);
        ArgumentNullException.ThrowIfNull(request);

        if (!_listings.TryGetValue(request.ListingId, out var listing))
        {
            return new MarketOperationResponse { Success = false, Message = "Listing not found." };
        }

        // restricted to seller ownership
        if (listing.SellerId != seller.Id)
        {
            SecurityLogger.LogSecurityEvent("MarketUnauthorizedCancelAttempt", seller.Id, $"ListingId:{request.ListingId}");
            return new MarketOperationResponse { Success = false, Message = "Unauthorized cancellation attempt." };
        }

        // Terminal states check
        if (listing.Status != MarketListingStatus.Active)
        {
            return new MarketOperationResponse { Success = false, Message = "Listing is no longer active." };
        }

        // transition is explicit
        listing.Status = MarketListingStatus.Cancelled;
        // Update persistence
        await _dbService.UpdateMarketListingStatusAsync(listing.ListingId, false);

        // Safe item return behavior
        seller.AddItem(listing.ItemId, listing.Quantity);

        SecurityLogger.LogSecurityEvent("MarketListingCancelled", seller.Id, $"ListingId:{listing.ListingId}");

        return new MarketOperationResponse
        {
            Success = true,
            ListingId = listing.ListingId,
            Message = "Listing cancelled successfully. Items returned to inventory."
        };
    }

    public async Task ExpireListingsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = _listings.Values.Where(l => l.Status == MarketListingStatus.Active && l.ExpiresUtc <= now).ToList();

        foreach (var listing in expired)
        {
            // Atomically update status to prevent double-expiration/idempotency
            bool shouldProcess = false;
            lock (listing)
            {
                if (listing.Status == MarketListingStatus.Active)
                {
                    listing.Status = MarketListingStatus.Expired;
                    shouldProcess = true;
                }
            }

            if (!shouldProcess) continue;

            // Update persistence (moved outside of lock)
            await _dbService.UpdateMarketListingStatusAsync(listing.ListingId, false);

            // Return items to the seller (handles online and offline cases via PlayerService)
            bool success = await _playerService.ReturnMarketItemAsync(listing.SellerId, listing.ItemId, listing.Quantity);

            if (success)
            {
                SecurityLogger.LogSecurityEvent("MarketListingExpired", listing.SellerId, $"ListingId:{listing.ListingId} ItemId:{listing.ItemId} Qty:{listing.Quantity} - Items returned.");
            }
            else
            {
                SecurityLogger.LogSecurityEvent("MarketListingExpirationError", listing.SellerId, $"ListingId:{listing.ListingId} - FAILED to return items!");
            }
        }
    }

    private async Task RecordSaleHistory(string listingId, int itemId, string itemName, long pricePerUnit, int quantity, long totalPrice, long gross, long tax, long net, int sellerId, int buyerId)
    {
        _saleHistory.Add(new SaleRecord
        {
            ItemId = itemId,
            PricePerUnit = pricePerUnit,
            Quantity = quantity,
            GrossAmount = gross,
            TaxAmount = tax,
            NetAmount = net,
            SellerId = sellerId,
            BuyerId = buyerId,
            SoldUtc = DateTime.UtcNow
        });

        // Persist to database
        await _dbService.RecordMarketTransactionAsync(listingId, buyerId, sellerId, itemId, itemName, quantity, pricePerUnit, totalPrice, gross, tax, net);
    }

    public MarketHistoryDTO GetPriceHistory(int itemId, string window = "7d")
    {
        // Parse window (e.g., "7d", "24h")
        var now = DateTime.UtcNow;
        var limit = window.ToLower() switch
        {
            "24h" => now.AddHours(-24),
            "7d" => now.AddDays(-7),
            "30d" => now.AddDays(-30),
            _ => now.AddDays(-7)
        };

        var history = _saleHistory
            .Where(s => s.ItemId == itemId && s.SoldUtc >= limit)
            .ToList();

        if (history.Count == 0)
        {
            return new MarketHistoryDTO
            {
                ItemId = itemId,
                MinPrice = 0,
                MaxPrice = 0,
                AveragePrice = 0,
                SampleCount = 0,
                Volume = 0,
                Window = window
            };
        }

        return new MarketHistoryDTO
        {
            ItemId = itemId,
            MinPrice = history.Min(s => s.PricePerUnit),
            MaxPrice = history.Max(s => s.PricePerUnit),
            AveragePrice = (long)history.Average(s => s.PricePerUnit),
            SampleCount = history.Count,
            Volume = history.Sum(s => s.Quantity),
            Window = window
        };
    }

    public MarketStatsDTO GetStats()
    {
        return new MarketStatsDTO
        {
            ActiveListingCount = _listings.Values.Count(l => l.Status == MarketListingStatus.Active),
            TotalGoldVolume = _saleHistory.Sum(s => s.GrossAmount),
            TotalTaxCollected = _saleHistory.Sum(s => s.TaxAmount),
            TotalItemsSold = _saleHistory.Sum(s => s.Quantity)
        };
    }

    public async Task InitializeAsync()
    {
        var dbListings = await _dbService.LoadActiveMarketListingsAsync();
        foreach (var dbListing in dbListings)
        {
            var listing = new MarketListingDTO
            {
                ListingId = dbListing.ListingId,
                SellerId = dbListing.SellerId,
                SellerName = "Unknown", // We'd need to join or load names if important
                ItemId = dbListing.ItemId,
                ItemName = dbListing.ItemName,
                Quantity = dbListing.Quantity,
                PricePerUnit = dbListing.PricePerUnit,
                TotalPrice = dbListing.TotalPrice,
                Status = MarketListingStatus.Active,
                CreatedUtc = dbListing.CreatedUtc,
                ExpiresUtc = dbListing.ExpiresUtc
            };
            _listings.TryAdd(listing.ListingId, listing);
        }
        Console.WriteLine($"[MarketManager] Initialized with {_listings.Count} active listings from DB.");
    }
}
