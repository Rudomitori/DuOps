using DuOps.Core.Registry;
using DuOps.Core.Storages;
using DuOps.Core.Telemetry;
using DuOps.Core.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.DependencyInjection;

public abstract class StorageBuilder
{
    public IServiceCollection Services { get; }
    public string StorageName { get; }

    protected StorageBuilder(IServiceCollection services, string storageName)
    {
        Services = services;
        StorageName = storageName;
    }

    public void AddWorkers(string queueName, int workerCount)
    {
        for (var i = 0; i < workerCount; i++)
        {
            Services.AddHostedService<Worker>(serviceProvider =>
            {
                var operationStorage = serviceProvider.GetRequiredKeyedService<IOperationStorage>(
                    StorageName
                );

                var logger = serviceProvider.GetRequiredService<ILogger<Worker>>();
                var registry = serviceProvider.GetRequiredService<OperationRegistry>();
                var telemetry = serviceProvider.GetRequiredService<IOperationTelemetry>();
                var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

                return new Worker(
                    operationStorage,
                    logger,
                    registry,
                    telemetry,
                    timeProvider,
                    serviceProvider,
                    queueName
                );
            });
        }
    }
}
