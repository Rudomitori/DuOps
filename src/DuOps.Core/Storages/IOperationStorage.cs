using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;

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

    Task AddInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult result,
        CancellationToken cancellationToken = default
    );

    Task SetState(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedOperationState state,
        CancellationToken cancellationToken = default
    );

    Task<OperationPollingScheduleId> GetOrSetPollingScheduleId(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken = default
    );

    Task Delete(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    );
}
