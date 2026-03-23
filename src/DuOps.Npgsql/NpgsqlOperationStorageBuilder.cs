using DuOps.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DuOps.Npgsql;

public class NpgsqlOperationStorageBuilder : StorageBuilder
{
    public OptionsBuilder<NpgsqlOperationStorageOptions> OptionsBuilder { get; }

    public NpgsqlOperationStorageBuilder(
        string storageName,
        IServiceCollection services,
        OptionsBuilder<NpgsqlOperationStorageOptions> optionsBuilder
    )
        : base(services, storageName)
    {
        OptionsBuilder = optionsBuilder;
    }

    public void UseNpgsqlDataSource()
    {
        Services.AddKeyedSingleton<IConnectionFactory, NpgsqlDataSourceConnectionFactory>(
            StorageName
        );
    }
}
