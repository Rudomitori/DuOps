using System.Collections.Concurrent;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Repositories;

namespace DuOps.Testing;

public sealed class InMemoryOperationStorage : IOperationStorage
{
    public readonly record struct OperationPk(
        OperationDiscriminator Discriminator,
        OperationId OperationId
    );

    public readonly ConcurrentDictionary<OperationPk, SerializedOperation> StoredOperations = new();

    public readonly ConcurrentDictionary<
        OperationPk,
        Dictionary<(InterResultDiscriminator Discriminator, string? Key), string>
    > StoredIntermediateResults = new();

    public async Task<SerializedOperation?> GetByIdOrDefault(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        var operationPk = new OperationPk(discriminator, operationId);

        return StoredOperations.GetValueOrDefault(operationPk);
    }

    public async Task<SerializedOperation> GetOrAdd(
        SerializedOperation serializedOperation,
        CancellationToken cancellationToken = default
    )
    {
        var operationPk = new OperationPk(serializedOperation.Discriminator, serializedOperation.Id);

        return StoredOperations.GetOrAdd(operationPk, serializedOperation);
    }

    public async Task<IReadOnlyCollection<SerializedInterResult>> GetInterResults(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        var operationPk = new OperationPk(discriminator, operationId);

        var results = StoredIntermediateResults.GetValueOrDefault(operationPk);
        return results
               ?.Select(x => new SerializedInterResult(
                            x.Key.Discriminator,
                            x.Key.Key,
                            x.Value
                        )
               )
               .ToArray() ?? [];
    }

    public async Task AddInterResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        SerializedInterResult interResult,
        CancellationToken cancellationToken
    )
    {
        var operationDiscriminator = discriminator;
        var operationPk = new OperationPk(operationDiscriminator, operationId);

        var resultDiscriminator = interResult.Discriminator;
        var resultKey = interResult.Key;

        StoredIntermediateResults.AddOrUpdate(
            operationPk,
            _ => new Dictionary<(InterResultDiscriminator Discriminator, string? Key), string>
            {
                [(resultDiscriminator, resultKey)] = interResult.Value,
            },
            (
                _,
                results
            ) =>
            {
                if (!results.TryAdd((resultDiscriminator, resultKey), interResult.Value))
                {
                    throw new InvalidOperationException(
                        $"Operation({operationId}.IntermediateResults[{resultDiscriminator}, {resultKey}] already exists"
                    );
                }

                return results;
            }
        );
    }

    public async Task AddResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        string serializedOperationResult,
        CancellationToken cancellationToken = default
    )
    {
        var operationPk = new OperationPk(discriminator, operationId);

        StoredOperations.AddOrUpdate(
            operationPk,
            _ => throw new InvalidOperationException(
                $"Operation {discriminator.Value} {operationId.Value} was not found"
            ),
            (
                _,
                operation
            ) =>
            {
                if (operation.ExecutionResult is OperationExecutionResult<string>.Finished)
                {
                    throw new InvalidOperationException(
                        $"Operation {discriminator.Value} {operationId.Value} already has result"
                    );
                }

                return operation with
                {
                    ExecutionResult = new OperationExecutionResult<string>.Finished(serializedOperationResult),
                };
            }
        );
    }

    public async Task<OperationPollingScheduleId> GetOrSetPollingScheduleId(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken
    )
    {
        var operationPk = new OperationPk(discriminator, operationId);

        var serializedOperation = StoredOperations.AddOrUpdate(
            operationPk,
            _ => throw new InvalidOperationException(
                $"Operation {discriminator.Value} {operationId.Value} was not found"
            ),
            (
                _,
                operation
            ) =>
            {
                if (operation.PollingScheduleId is not null)
                {
                    return operation;
                }

                return operation with { PollingScheduleId = scheduleId };
            }
        );

        return serializedOperation.PollingScheduleId!.Value;
    }

    public Task Delete(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
}
