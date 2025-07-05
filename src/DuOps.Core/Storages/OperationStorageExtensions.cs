using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Repositories;

namespace DuOps.Core.Storages;

public static class OperationStorageExtensions
{
    public static async Task<Operation<TArgs, TResult>?> GetByIdOrDefault<TArgs, TResult>(
        this IOperationStorage storage,
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperation = await storage.GetByIdOrDefault(
            operationDefinition.Discriminator,
            operationId,
            cancellationToken
        );
        return serializedOperation is not null
            ? operationDefinition.Deserialize(serializedOperation)
            : null;
    }

    public static async Task<Operation<TArgs, TResult>> GetOrAdd<TArgs, TResult>(
        this IOperationStorage storage,
        IOperationDefinition<TArgs, TResult> operationDefinition,
        Operation<TArgs, TResult> operation,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperation = operationDefinition.Serialize(operation);
        serializedOperation = await storage.GetOrAdd(serializedOperation, cancellationToken);
        return operationDefinition.Deserialize(serializedOperation);
    }
}
