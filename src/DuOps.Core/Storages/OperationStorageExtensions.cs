using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.Storages;

public static class OperationStorageExtensions
{
    public static async Task<Operation<TId, TArgs, TResult>?> GetByIdOrDefaultAsync<
        TId,
        TArgs,
        TResult
    >(
        this IOperationStorage storage,
        IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        TId operationId,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperationId = operationDefinition.SerializeId(operationId);

        var serializedOperation = await storage.GetByIdOrDefaultAsync(
            operationDefinition.Type,
            serializedOperationId,
            cancellationToken
        );
        return serializedOperation is not null
            ? operationDefinition.Deserialize(serializedOperation)
            : null;
    }

    public static Task ScheduleOperationAsync<TId, TArgs, TResult>(
        this IOperationStorage storage,
        IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        string queue,
        TId operationId,
        TArgs operationArgs,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperationId = operationDefinition.SerializeId(operationId);
        var serializedOperationArgs = operationDefinition.SerializeArgs(operationArgs);

        return storage.ScheduleOperationAsync(
            operationDefinition.Type,
            queue,
            serializedOperationId,
            serializedOperationArgs,
            cancellationToken
        );
    }
}
