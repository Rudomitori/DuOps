using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Storages;

public interface IOperationStorageHandle : IAsyncDisposable
{
    OperationType OperationType { get; }
    SerializedOperationId SerializedOperationId { get; }
    SerializedOperationArgs OperationArgs { get; }
    int RetryCount { get; }

    SerializedInnerResult? GetInnerResultOrDefault(
        InnerResultType type,
        SerializedInnerResultId? id
    );

    SerializedInnerResult[] GetInnerResults(InnerResultType type);

    Task CompleteAsync(SerializedOperationResult result, CancellationToken cancellationToken);

    Task FailAsync(string reason, CancellationToken cancellationToken);

    Task RescheduleAsync(DateTime at, CancellationToken cancellationToken);

    Task ScheduleRetryAsync(DateTime retryAt, CancellationToken cancellationToken);

    void AddInnerResultLazy(SerializedInnerResult innerResult);

    Task FlushInnerResultsAsync();
}
