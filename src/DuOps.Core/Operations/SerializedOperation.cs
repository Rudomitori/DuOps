using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Operations;

public sealed record SerializedOperation(
    OperationType Type,
    SerializedOperationId Id,
    string Queue,
    DateTime? ScheduledAt,
    SerializedOperationArgs Args,
    DateTime CreatedAt,
    SerializedOperationState State,
    int RetryCount
);
