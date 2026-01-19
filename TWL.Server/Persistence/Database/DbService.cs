using Npgsql;
using System.Threading.Tasks;

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
    public void Init()
    {
        Console.WriteLine("Initializing database...");
        InitDatabase();
        Console.WriteLine("Database initialized successfully.");
    }

    public void InitDatabase()
    {
        // Ejemplo: crear tablas si no existen
        using var con = new NpgsqlConnection(_connString);
        con.Open();
        using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS accounts (
                    user_id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    pass_hash VARCHAR(128) NOT NULL
                );
                CREATE TABLE IF NOT EXISTS players (
                    player_id SERIAL PRIMARY KEY,
                    user_id INT REFERENCES accounts(user_id),
                    pos_x FLOAT,
                    pos_y FLOAT,
                    hp INT,
                    max_hp INT
                    -- mas campos, etc.
                );
            ", con);
        cmd.ExecuteNonQuery();
    }

    // Ejemplo: login
    public async Task<int> CheckLoginAsync(string username, string passHash)
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
        if (result == null) return -1;
        return Convert.ToInt32(result);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                // Dispose managed resources if needed
                Console.WriteLine("Database connection disposed.");

            // Free unmanaged resources if any
            _disposed = true;
        }
    }

    ~DbService()
    {
        Dispose(false);
    }
}