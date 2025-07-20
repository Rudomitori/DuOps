using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults;

namespace DuOps.Core.Operations;

public sealed record Operation<TArgs, TResult>(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime CreatedAt,
    TArgs Args,
    OperationState<TResult> State,
    IReadOnlyCollection<SerializedInterResult> SerializedInterResults
);
