using System.Data;

namespace TWL.Server.Persistence.Database;

public interface IDapperService
{
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
}
