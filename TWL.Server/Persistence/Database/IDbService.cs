using Npgsql;

namespace TWL.Server.Persistence.Database;

public interface IDbService : IDisposable
{
    void Init();
    void InitDatabase();
    Task<int> CheckLoginAsync(string username, string passHash);
    Task<bool> CheckHealthAsync();
    Task<T> ExecuteSerializableAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work);
    Task RecordMarketTransactionAsync(string listingId, int buyerId, int sellerId, int itemId, string itemName, int quantity, long pricePerUnit, long totalPrice, long grossAmount, long taxAmount, long netAmount);
    
    // Market Listings Persistence
    Task CreateMarketListingAsync(string listingId, int sellerId, int itemId, string itemName, int quantity, long pricePerUnit, long totalPrice, DateTime expiresUtc);
    Task UpdateMarketListingStatusAsync(string listingId, bool isActive);
    Task<List<MarketListingPersistenceData>> LoadActiveMarketListingsAsync();
}

public class MarketListingPersistenceData
{
    public string ListingId { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long PricePerUnit { get; set; }
    public long TotalPrice { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public bool IsActive { get; set; }
}
