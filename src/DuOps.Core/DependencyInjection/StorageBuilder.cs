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
    public OperationStorageId StorageId { get; }

    protected StorageBuilder(IServiceCollection services, OperationStorageId storageId)
    {
        Services = services;
        StorageId = storageId;
    }

    public void AddWorkers(OperationQueueId queueId, int workerCount)
    {
        for (var i = 0; i < workerCount; i++)
        {
            Services.AddHostedService<Worker>(serviceProvider =>
            {
                var operationStorage = serviceProvider.GetRequiredKeyedService<IOperationStorage>(
                    StorageId
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
                    queueId
                );
            });
        }
    }
}
