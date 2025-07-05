using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Storages;

public interface IOperationStorage
{
    Task<SerializedOperation?> GetByIdOrDefault(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    );

    Task<SerializedOperation> GetOrAdd(
        SerializedOperation serializedOperation,
        CancellationToken cancellationToken = default
    );

    Task<
        IReadOnlyDictionary<
            (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
            SerializedInterResult
        >
    > GetInterResults(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    );

    Task AddInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        SerializedInterResultKey? key,
        SerializedInterResult result,
        CancellationToken cancellationToken
    );

    Task AddResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        SerializedOperationResult serializedOperationResult,
        CancellationToken cancellationToken = default
    );

    Task<OperationPollingScheduleId> GetOrSetPollingScheduleId(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken
    );

    Task Delete(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    );
}
