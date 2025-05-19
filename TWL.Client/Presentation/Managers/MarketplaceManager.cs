using System.Collections.Generic;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers;

public class Listing
{
    public int ItemId;
    public int ListingId;
    public int Price;
    public int Quantity;
    public int SellerId;
}

public class MarketplaceManager
{
    private readonly Dictionary<int, Listing> _listings; // listingID -> Listing
    private int _nextListingId;

    public MarketplaceManager()
    {
        _listings = new Dictionary<int, Listing>();
        _nextListingId = 1;
    }

    public int CreateListing(int sellerId, int itemId, int price, int qty)
    {
        var listing = new Listing
        {
            ListingId = _nextListingId++,
            SellerId = sellerId,
            ItemId = itemId,
            Price = price,
            Quantity = qty
        };
        _listings.Add(listing.ListingId, listing);
        return listing.ListingId;
    }

    public bool BuyListing(int buyerId, int listingId, int qty, Inventory buyerInventory, Inventory sellerInventory,
        ref string status)
    {
        if (!_listings.ContainsKey(listingId))
        {
            status = "Listing not found.";
            return false;
        }

        var listing = _listings[listingId];
        if (listing.Quantity < qty)
        {
            status = "Not enough quantity in listing.";
            return false;
        }

        var cost = listing.Price * qty;
        // Check if buyer has enough gold
        if (buyerInventory.GetItemCount(1) < cost) // supongamos itemID=1 = Gold
        {
            status = "Buyer has insufficient gold.";
            return false;
        }

        // Buyer pays gold
        buyerInventory.RemoveItem(1, cost);

        // Seller receives gold
        sellerInventory.AddItem(1, cost);

        // Transfer item
        // Se asume que el seller ya extrajo el item al crear listing, o lo mantiene aparte
        // Simplificado: buyer gets item
        buyerInventory.AddItem(listing.ItemId, qty);
        listing.Quantity -= qty;

        if (listing.Quantity <= 0) _listings.Remove(listingId);

        status = "Purchase successful!";
        return true;
    }

    public IReadOnlyCollection<Listing> GetAllListings()
    {
        return _listings.Values;
    }
}