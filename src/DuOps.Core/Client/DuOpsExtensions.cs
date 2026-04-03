using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;

namespace DuOps.Core.Client;

public static class DuOpsExtensions
{
    public static async Task<Operation<TId, TArgs, TResult>?> GetOperationByIdOrDefaultAsync<
        TId,
        TArgs,
        TResult
    >(
        this IDuOpsClient client,
        IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        OperationStorageId storageId,
        TId operationId,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperationId = operationDefinition.SerializeId(operationId);

        var serializedOperation = await client.GetOperationByIdOrDefaultAsync(
            storageId,
            operationDefinition.Type,
            serializedOperationId,
            cancellationToken
        );
        return serializedOperation is not null
            ? operationDefinition.Deserialize(serializedOperation)
            : null;
    }

    public static Task ScheduleOperationAsync<TId, TArgs, TResult>(
        this IDuOpsClient client,
        IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        OperationStorageId storageId,
        OperationQueueId queueId,
        TId operationId,
        TArgs operationArgs,
        CancellationToken cancellationToken = default
    )
    {
        var serializedOperationId = operationDefinition.SerializeId(operationId);
        var serializedOperationArgs = operationDefinition.SerializeArgs(operationArgs);

        return client.ScheduleOperationAsync(
            storageId,
            queueId,
            operationDefinition.Type,
            serializedOperationId,
            serializedOperationArgs,
            cancellationToken
        );
    }
}
