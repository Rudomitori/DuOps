using System.Diagnostics;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

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

    public static async Task AddResult<TArgs, TResult>(
        this IOperationStorage storage,
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        TResult result,
        CancellationToken cancellationToken = default
    )
    {
        var serializedResult = operationDefinition.SerializeResultAndWrapException(result);
        await storage.AddResult(
            operationDefinition.Discriminator,
            operationId,
            serializedResult,
            cancellationToken
        );
    }

    public static async Task<SerializedOperation?> AwaitOperationAndGetByIdOrDefault(
        this IOperationStorage storage,
        OperationDiscriminator discriminator,
        OperationId operationId,
        TimeSpan attemptsInterval,
        TimeSpan maxWaitTime,
        CancellationToken cancellationToken = default
    )
    {
        var stopWatch = Stopwatch.StartNew();

        var operation = await storage.GetByIdOrDefault(
            discriminator,
            operationId,
            cancellationToken
        );

        while (operation is null && stopWatch.Elapsed + attemptsInterval < maxWaitTime)
        {
            await Task.Delay(attemptsInterval, cancellationToken);
            operation = await storage.GetByIdOrDefault(
                discriminator,
                operationId,
                cancellationToken
            );
        }

        return operation;
    }
}
