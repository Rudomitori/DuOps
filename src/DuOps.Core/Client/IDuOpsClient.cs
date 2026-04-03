using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;

namespace DuOps.Core.Client;

public interface IDuOpsClient
{
    Task ScheduleOperationAsync(
        OperationStorageId storageId,
        OperationQueueId queueId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        SerializedOperationArgs serializedOperationArgs,
        CancellationToken cancellationToken = default
    );

    Task<SerializedOperation?> GetOperationByIdOrDefaultAsync(
        OperationStorageId storageId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    );

    Task<SerializedInnerResult[]> GetAllInnerResultsAsync(
        OperationStorageId storageId,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    );
}
