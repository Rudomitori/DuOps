using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.Client;

internal sealed class DuOpsClient(IServiceProvider serviceProvider) : IDuOpsClient
{
    public async Task ScheduleOperationAsync(
        OperationStorageId storageId,
        OperationQueueId queueId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        SerializedOperationArgs serializedOperationArgs,
        CancellationToken cancellationToken = default
    )
    {
        var storage = serviceProvider.GetRequiredKeyedService<IOperationStorage>(storageId);

        await storage.ScheduleOperationAsync(
            operationType,
            queueId,
            serializedOperationId,
            serializedOperationArgs,
            cancellationToken
        );
    }

    public async Task<SerializedOperation?> GetOperationByIdOrDefaultAsync(
        OperationStorageId storageId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    )
    {
        var storage = serviceProvider.GetRequiredKeyedService<IOperationStorage>(storageId);

        return await storage.GetByIdOrDefaultAsync(
            operationType,
            serializedOperationId,
            cancellationToken
        );
    }

    public async Task<SerializedInnerResult[]> GetAllInnerResultsAsync(
        OperationStorageId storageId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    )
    {
        var storage = serviceProvider.GetRequiredKeyedService<IOperationStorage>(storageId);

        return await storage.GetAllInnerResultsAsync(
            operationType,
            serializedOperationId,
            cancellationToken
        );
    }
}
