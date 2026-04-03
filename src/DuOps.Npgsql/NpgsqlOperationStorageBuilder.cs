using DuOps.Core.DependencyInjection;
using DuOps.Core.Storages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DuOps.Npgsql;

public class NpgsqlOperationStorageBuilder : StorageBuilder
{
    public OptionsBuilder<NpgsqlOperationStorageOptions> OptionsBuilder { get; }

    public NpgsqlOperationStorageBuilder(
        OperationStorageId storageId,
        IServiceCollection services,
        OptionsBuilder<NpgsqlOperationStorageOptions> optionsBuilder
    )
        : base(services, storageId)
    {
        OptionsBuilder = optionsBuilder;
    }

    public void UseNpgsqlDataSource()
    {
        Services.AddKeyedSingleton<IConnectionFactory, NpgsqlDataSourceConnectionFactory>(
            StorageId
        );
    }
}
