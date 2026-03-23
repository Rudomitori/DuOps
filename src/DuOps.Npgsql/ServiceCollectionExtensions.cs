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
        string storageName,
        Action<NpgsqlOperationStorageBuilder> configure
    )
    {
        var optionsBuilder = builder.Services.AddOptions<NpgsqlOperationStorageOptions>(
            storageName
        );

        builder.Services.AddNpgsqlOperationStorage(storageName);

        var storageBuilder = new NpgsqlOperationStorageBuilder(
            storageName,
            builder.Services,
            optionsBuilder
        );

        configure(storageBuilder);
    }

    private static void AddNpgsqlOperationStorage(
        this IServiceCollection services,
        string storageName
    )
    {
        services.AddKeyedSingleton<IOperationStorage, NpgsqlOperationStorage>(
            storageName,
            (serviceProvider, _) =>
            {
                var connectionFactory = serviceProvider.GetRequiredKeyedService<IConnectionFactory>(
                    storageName
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
                    storageName
                );
            }
        );
    }
}
