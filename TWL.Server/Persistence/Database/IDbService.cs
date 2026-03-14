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
}
