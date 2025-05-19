using TWL.Shared.Domain.Models;

namespace TWL.Shared.Domain.Requests;

public class MarketplaceUpdate
{
    public MarketplaceUpdate()
    {
        ItemsForSale = new List<Item>();
    }

    public List<Item> ItemsForSale { get; set; }
}