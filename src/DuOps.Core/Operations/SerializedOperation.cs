using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations;

public sealed record SerializedOperation(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime StartedAt,
    string Args,
    OperationExecutionResult<string> ExecutionResult,
    IReadOnlyDictionary<
        (InterResultDiscriminator Discriminator, string? Key),
        string
    > SerializedMetaData
);
