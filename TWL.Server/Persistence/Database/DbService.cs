using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TWL.Server.Persistence.Database;

public class DbService : IDisposable
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

    // Method called by GameServer.Start()
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
