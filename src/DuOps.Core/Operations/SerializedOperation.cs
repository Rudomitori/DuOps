using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults;

namespace DuOps.Core.Operations;

public sealed record SerializedOperation(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime StartedAt,
    SerializedOperationArgs Args,
    SerializedOperationState State,
    IReadOnlyCollection<SerializedInterResult> InterResults
);
