using DuOps.Core.DependencyInjection;
using DuOps.Core.Storages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DuOps.Npgsql;

public static class ServiceCollectionExtensions
{
    public static void AddNpgsqlOperationStorage(
        this DuOpsBuilder builder,
        OperationStorageId storageId,
        Action<NpgsqlOperationStorageBuilder> configure
    )
    {
        var optionsBuilder = builder.Services.AddOptions<NpgsqlOperationStorageOptions>(storageId);

        builder.Services.AddNpgsqlOperationStorage(storageId);

        var storageBuilder = new NpgsqlOperationStorageBuilder(
            storageId,
            builder.Services,
            optionsBuilder
        );

        configure(storageBuilder);
    }

    private static void AddNpgsqlOperationStorage(
        this IServiceCollection services,
        OperationStorageId storageId
    )
    {
        services.AddKeyedSingleton<IOperationStorage, NpgsqlOperationStorage>(
            storageId,
            (serviceProvider, _) =>
            {
                var connectionFactory = serviceProvider.GetRequiredKeyedService<IConnectionFactory>(
                    storageId
                );
                var optionsMonitor = serviceProvider.GetRequiredService<
                    IOptionsMonitor<NpgsqlOperationStorageOptions>
                >();
                var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                var logger = serviceProvider.GetRequiredService<ILogger<NpgsqlOperationStorage>>();

                return new NpgsqlOperationStorage(
                    connectionFactory,
                    optionsMonitor,
                    timeProvider,
                    logger,
                    storageId
                );
            }
        );
    }
}
