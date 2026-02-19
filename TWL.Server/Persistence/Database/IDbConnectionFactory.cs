using System.Data.Common;

namespace TWL.Server.Persistence.Database;

public interface IDbConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly Npgsql.NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(Npgsql.NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
