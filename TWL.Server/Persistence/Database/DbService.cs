using Npgsql;

namespace TWL.Server.Persistence.Database;

public class DbService : IDisposable
{
    private readonly string _connString;
    private bool _disposed;

    public DbService(string connString)
    {
        _connString = connString;
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
        InitDatabase();
        Console.WriteLine("Database initialized successfully.");
    }

    public virtual void InitDatabase()
    {
        // Database initialization is now handled by EF Core migrations.
        Console.WriteLine("Database initialization skipped (EF Core in use).");
    }

    // Ejemplo: login
    public virtual async Task<int> CheckLoginAsync(string username, string passHash)
    {
        await using var con = new NpgsqlConnection(_connString);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
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
                // Dispose managed resources if needed
            {
                Console.WriteLine("Database connection disposed.");
            }

            // Free unmanaged resources if any
            _disposed = true;
        }
    }

    ~DbService()
    {
        Dispose(false);
    }
}