using System.Data.Common;
using Npgsql;

namespace DuOps.Npgsql;

public sealed class NpgsqlDataSourceConnectionFactory(NpgsqlDataSource dataSource)
    : IConnectionFactory
{
    public async ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        return await dataSource.OpenConnectionAsync(cancellationToken);
    }
}
