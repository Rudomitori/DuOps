using System.Data.Common;

namespace DuOps.Npgsql;

public interface IConnectionFactory
{
    ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
