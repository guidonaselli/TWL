using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
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
        if (transaction != null)
        {
            return await transaction.Connection!.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        if (transaction != null)
        {
            return await transaction.Connection!.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync();
        return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }
}
