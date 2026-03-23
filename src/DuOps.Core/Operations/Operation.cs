using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Operations;

public sealed record Operation<TId, TArgs, TResult>(
    OperationType Type,
    TId Id,
    OperationQueueId QueueId,
    DateTime? ScheduledAt,
    TArgs Args,
    DateTime CreatedAt,
    OperationState<TResult> State,
    int RetryCount
);
