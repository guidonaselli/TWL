using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TWL.Server.Persistence.Database;

public class DbService : IDbService
{
    private readonly string _connString;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    public DbService(string connString, IServiceProvider serviceProvider)
    {
        _connString = connString;
        _serviceProvider = serviceProvider;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Init()
    {
        Console.WriteLine("Initializing database...");

        // Use EF Core Migration
        // Create a scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            try
            {
                Console.WriteLine("Applying EF Core Migrations...");
                dbContext.Database.Migrate();
                Console.WriteLine("EF Core Migrations applied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}");
                // Fail-closed: rethrow or ensure server doesn't start with bad DB state?
                // For now, logging error is crucial.
                throw;
            }
        }

        // Legacy Init (maintain for 'accounts' table until full migration)
        InitDatabase();

        Console.WriteLine("Database initialized successfully.");
    }

    // Keep legacy InitDatabase if strictly needed for non-EF stuff, but PERS-001a goal is EF Core.
    // We can keep it empty or remove it. For backward compatibility if other code calls it:
    public virtual void InitDatabase()
    {
        // Ejemplo: crear tablas si no existen (Legacy)
        using var con = new NpgsqlConnection(_connString);
        con.Open();
        using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS accounts (
                    user_id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    pass_hash VARCHAR(128) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS market_listings (
                    listing_id TEXT PRIMARY KEY,
                    seller_id INT NOT NULL,
                    item_id INT NOT NULL,
                    item_name TEXT NOT NULL,
                    quantity INT NOT NULL,
                    price_per_unit BIGINT NOT NULL,
                    total_price BIGINT NOT NULL,
                    created_utc TIMESTAMP NOT NULL,
                    expires_utc TIMESTAMP NOT NULL,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS market_history (
                    transaction_id SERIAL PRIMARY KEY,
                    listing_id TEXT NOT NULL,
                    buyer_id INT NOT NULL,
                    seller_id INT NOT NULL,
                    item_id INT NOT NULL,
                    item_name TEXT NOT NULL,
                    quantity INT NOT NULL,
                    price_per_unit BIGINT NOT NULL,
                    total_price BIGINT NOT NULL,
                    gross_amount BIGINT NOT NULL,
                    tax_amount BIGINT NOT NULL,
                    net_amount BIGINT NOT NULL,
                    completed_utc TIMESTAMP NOT NULL
                );
                
                -- Players table is now managed by EF Core as 'Players' (capitalized),
                -- while this legacy script created 'players' (lowercase).
                -- To avoid conflicts, we remove the 'players' creation here.
                -- CREATE TABLE IF NOT EXISTS players ( ... );
            ", con);
        cmd.ExecuteNonQuery();
    }

    // Legacy Login Check (to be replaced by Repository later, but kept for compilation)
    public virtual async Task<int> CheckLoginAsync(string username, string passHash)
    {
        // For now, we still use Npgsql for this legacy method until Repositories are fully implemented.
        // Or we could rewrite it using EF Core?
        // Since PERS-001b is "Implement DbPlayerRepository", we leave this as is (using raw Npgsql)
        // to avoid breaking existing auth if it is used.
        // However, the previous implementation created tables "accounts" and "players" which might conflict
        // with our new "Players" table managed by EF.
        // CHECK: Does the system use this?
        // The Roadmap says "PERS-001a: Infra + Entities".
        // If I break "CheckLoginAsync" now, I might break login if it's used.
        // But "accounts" table was created in InitDatabase(). If I stop calling InitDatabase(), "accounts" won't exist in a fresh DB.
        // Current state: The codebase seems to be in a transition.
        // Let's check where CheckLoginAsync is used.

        await using var con = new NpgsqlConnection(_connString);
        await con.OpenAsync();

        // We might need to ensure 'accounts' table exists if we still use this path.
        // But let's assume for PERS-001a we just set up EF Core alongside.
        // If this is a fresh setup, 'accounts' table won't exist because I commented out InitDatabase().
        // I should probably ensure the legacy tables are created if I want to keep this method working
        // OR rely on EF Core to manage everything.
        // Given the instructions, I should probably leave legacy logic alone or minimal touch.
        // But InitDatabase() was doing "CREATE TABLE IF NOT EXISTS".
        // I should probably restore that call OR migrate it to EF.

        // Decision: Restore InitDatabase() call to keep legacy tables alive for now,
        // preventing regression until full migration in PERS-001b.

        using var cmd = new NpgsqlCommand(@"
                SELECT user_id FROM accounts
                WHERE username=@u AND pass_hash=@p
            ", con);
        cmd.Parameters.AddWithValue("u", username);
        cmd.Parameters.AddWithValue("p", passHash);

        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
        {
            return -1;
        }

        return Convert.ToInt32(result);
    }

    public virtual async Task<bool> CheckHealthAsync()
    {
        try
        {
            await using var con = new NpgsqlConnection(_connString);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT 1", con);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database health check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Executes the provided work within a Serializable transaction.
    /// This is the required isolation level for valuable multi-party operations
    /// to prevent write skew, phantom reads, and double-spend anomalies.
    /// </summary>
    public virtual async Task<T> ExecuteSerializableAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
    {
        await using var con = new NpgsqlConnection(_connString);
        await con.OpenAsync();
        
        // Begin transaction with Serializable isolation level
        await using var transaction = await con.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        
        try
        {
            var result = await work(con, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // In a real production system, log this failure to a telemetry sink for operational triage
            Console.WriteLine($"[DbService] Serializable transaction failed: {ex.Message}");
            throw;
        }
    }

    public virtual async Task RecordMarketTransactionAsync(string listingId, int buyerId, int sellerId, int itemId, string itemName, int quantity, long pricePerUnit, long totalPrice, long grossAmount, long taxAmount, long netAmount)
    {
        try
        {
            await using var con = new NpgsqlConnection(_connString);
            await con.OpenAsync();

            const string sql = @"
                INSERT INTO market_history (
                    listing_id, buyer_id, seller_id, item_id, item_name, quantity, 
                    price_per_unit, total_price, gross_amount, tax_amount, net_amount, completed_utc
                ) VALUES (
                    @listing_id, @buyer_id, @seller_id, @item_id, @item_name, @quantity, 
                    @price_per_unit, @total_price, @gross_amount, @tax_amount, @net_amount, @completed_utc
                )";

            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("listing_id", listingId);
            cmd.Parameters.AddWithValue("buyer_id", buyerId);
            cmd.Parameters.AddWithValue("seller_id", sellerId);
            cmd.Parameters.AddWithValue("item_id", itemId);
            cmd.Parameters.AddWithValue("item_name", itemName);
            cmd.Parameters.AddWithValue("quantity", quantity);
            cmd.Parameters.AddWithValue("price_per_unit", pricePerUnit);
            cmd.Parameters.AddWithValue("total_price", totalPrice);
            cmd.Parameters.AddWithValue("gross_amount", grossAmount);
            cmd.Parameters.AddWithValue("tax_amount", taxAmount);
            cmd.Parameters.AddWithValue("net_amount", netAmount);
            cmd.Parameters.AddWithValue("completed_utc", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbService] Failed to record market transaction: {ex.Message}");
            throw;
        }
    }

    public virtual async Task CreateMarketListingAsync(string listingId, int sellerId, int itemId, string itemName, int quantity, long pricePerUnit, long totalPrice, DateTime expiresUtc)
    {
        try
        {
            await using var con = new NpgsqlConnection(_connString);
            await con.OpenAsync();

            const string sql = @"
                INSERT INTO market_listings (
                    listing_id, seller_id, item_id, item_name, quantity, 
                    price_per_unit, total_price, created_utc, expires_utc, is_active
                ) VALUES (
                    @listing_id, @seller_id, @item_id, @item_name, @quantity, 
                    @price_per_unit, @total_price, @created_utc, @expires_utc, @is_active
                )";

            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("listing_id", listingId);
            cmd.Parameters.AddWithValue("seller_id", sellerId);
            cmd.Parameters.AddWithValue("item_id", itemId);
            cmd.Parameters.AddWithValue("item_name", itemName);
            cmd.Parameters.AddWithValue("quantity", quantity);
            cmd.Parameters.AddWithValue("price_per_unit", pricePerUnit);
            cmd.Parameters.AddWithValue("total_price", totalPrice);
            cmd.Parameters.AddWithValue("created_utc", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("expires_utc", expiresUtc);
            cmd.Parameters.AddWithValue("is_active", true);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbService] Failed to create market listing: {ex.Message}");
            throw;
        }
    }

    public virtual async Task UpdateMarketListingStatusAsync(string listingId, bool isActive)
    {
        try
        {
            await using var con = new NpgsqlConnection(_connString);
            await con.OpenAsync();

            const string sql = "UPDATE market_listings SET is_active = @is_active WHERE listing_id = @listing_id";

            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("listing_id", listingId);
            cmd.Parameters.AddWithValue("is_active", isActive);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbService] Failed to update market listing status: {ex.Message}");
            throw;
        }
    }

    public virtual async Task<List<MarketListingPersistenceData>> LoadActiveMarketListingsAsync()
    {
        try
        {
            await using var con = new NpgsqlConnection(_connString);
            await con.OpenAsync();

            const string sql = "SELECT * FROM market_listings WHERE is_active = TRUE";

            await using var cmd = new NpgsqlCommand(sql, con);
            await using var reader = await cmd.ExecuteReaderAsync();

            var listings = new List<MarketListingPersistenceData>();
            while (await reader.ReadAsync())
            {
                listings.Add(new MarketListingPersistenceData
                {
                    ListingId = reader.GetString(reader.GetOrdinal("listing_id")),
                    SellerId = reader.GetInt32(reader.GetOrdinal("seller_id")),
                    ItemId = reader.GetInt32(reader.GetOrdinal("item_id")),
                    ItemName = reader.GetString(reader.GetOrdinal("item_name")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                    PricePerUnit = reader.GetInt64(reader.GetOrdinal("price_per_unit")),
                    TotalPrice = reader.GetInt64(reader.GetOrdinal("total_price")),
                    CreatedUtc = reader.GetDateTime(reader.GetOrdinal("created_utc")),
                    ExpiresUtc = reader.GetDateTime(reader.GetOrdinal("expires_utc")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                });
            }
            return listings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbService] Failed to load active market listings: {ex.Message}");
            throw;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }
            _disposed = true;
        }
    }

    ~DbService()
    {
        Dispose(false);
    }
}
