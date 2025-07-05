using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations;

public sealed record SerializedOperation(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime StartedAt,
    SerializedOperationArgs Args,
    SerializedOperationState State
);
