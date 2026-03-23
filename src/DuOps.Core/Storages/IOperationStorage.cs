using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.Storages;

public interface IOperationStorage
{
    IAsyncEnumerable<IOperationStorageHandle> EnumerateForExecutionAsync(
        OperationQueueId queueId,
        CancellationToken cancellationToken = default
    );

    Task<SerializedOperation?> GetByIdOrDefaultAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    );

    Task ScheduleOperationAsync(
        OperationType operationType,
        OperationQueueId queueId,
        SerializedOperationId serializedOperationId,
        SerializedOperationArgs serializedOperationArgs,
        CancellationToken cancellationToken = default
    );

    Task<SerializedInnerResult[]> GetAllInnerResultsAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    );
}
