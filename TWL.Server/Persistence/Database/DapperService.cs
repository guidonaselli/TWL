using System.Data;
using Dapper;

namespace TWL.Server.Persistence.Database;

public class DapperService : IDapperService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        // If a transaction is provided, use its connection.
        // Dapper extension methods extend IDbConnection.
        if (transaction != null)
        {
            return await transaction.Connection!.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        // Otherwise open a new connection
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }
}
