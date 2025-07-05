using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Operations;

public sealed record Operation<TArgs, TResult>(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime CreatedAt,
    OperationArgs<TArgs> Args,
    OperationState<TResult> State
) : Operation(Id, PollingScheduleId, CreatedAt);

public abstract record Operation(
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime CreatedAt
);
