using System.Collections.Generic;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.DTO;

namespace TWL.Shared.Domain.Requests;

public class MarketplaceUpdate
{
    public MarketplaceUpdate()
    {
        ItemsForSale = new List<MarketListingDTO>();
        History = new List<MarketHistoryDTO>();
    }

    public List<MarketListingDTO> ItemsForSale { get; set; }
    public List<MarketHistoryDTO> History { get; set; }
}