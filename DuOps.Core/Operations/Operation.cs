using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations;

public sealed record Operation<TArgs, TResult>(
    OperationDiscriminator Discriminator,
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime CreatedAt,
    IReadOnlyDictionary<(InterResultDiscriminator Discriminator, string? Key), string> SerializedMetaData,
    TArgs Arguments,
    OperationExecutionResult<TResult> ExecutionResult
) : Operation(Id, PollingScheduleId, CreatedAt, SerializedMetaData);

public abstract record Operation(
    OperationId Id,
    OperationPollingScheduleId? PollingScheduleId,
    DateTime CreatedAt,
    IReadOnlyDictionary<(InterResultDiscriminator Discriminator, string? Key), string> SerializedMetaData
);
