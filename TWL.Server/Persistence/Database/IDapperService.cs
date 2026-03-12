using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace TWL.Server.Persistence.Database;

public interface IDapperService
{
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
}
