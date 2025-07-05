using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;

namespace DuOps.Core.Repositories;

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

    Task<IReadOnlyCollection<SerializedInterResult>> GetInterResults(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    );

    Task AddInterResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        SerializedInterResult interResult,
        CancellationToken cancellationToken
    );

    Task AddResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        string serializedOperationResult,
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
