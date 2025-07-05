using DuOps.Core.Exceptions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationDefinitions;

public static class OperationDefinitionExtensions
{
    public static Operation<TArgs, TResult> NewOperation<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId id,
        TArgs args,
        DateTime createdAt
    )
    {
        return new Operation<TArgs, TResult>(
            operationDefinition.Discriminator,
            id,
            PollingScheduleId: null,
            createdAt,
            SerializedMetaData: new Dictionary<(InterResultDiscriminator Discriminator, string? Key), string>(),
            args,
            OperationExecutionResult<TResult>.Yielded.Instance
        );
    }

    public static SerializedOperation Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        Operation<TArgs, TResult> operation
    )
    {
        return new SerializedOperation(
            operationDefinition.Discriminator,
            operation.Id,
            operation.PollingScheduleId,
            operation.CreatedAt,
            operationDefinition.SerializeArgs(operation.Arguments),
            operationDefinition.Serialize(operation.ExecutionResult),
            operation.SerializedMetaData
        );
    }

    public static OperationExecutionResult<string> Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationExecutionResult<TResult> executionResult
    )
    {
        return executionResult switch
        {
            OperationExecutionResult<TResult>.Yielded => OperationExecutionResult<string>.Yielded.Instance,
            OperationExecutionResult<TResult>.Finished { Result: var result } =>
                new OperationExecutionResult<string>.Finished(operationDefinition.SerializeResult(result)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(executionResult),
                executionResult,
                "Unknow operation execution result type"
            ),
        };
    }

    public static Operation<TArgs, TResult> Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation
    )
    {
        return new Operation<TArgs, TResult>(
            operationDefinition.Discriminator,
            serializedOperation.Id,
            serializedOperation.PollingScheduleId,
            serializedOperation.StartedAt,
            serializedOperation.SerializedMetaData,
            operationDefinition.DeserializeArgs(serializedOperation.Args),
            operationDefinition.Deserialize(serializedOperation.ExecutionResult)
        );
    }

    public static OperationExecutionResult<TResult> Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationExecutionResult<string> executionResult
    )
    {
        return executionResult switch
        {
            OperationExecutionResult<string>.Yielded => OperationExecutionResult<TResult>.Yielded.Instance,
            OperationExecutionResult<string>.Finished { Result: var result } =>
                new OperationExecutionResult<TResult>.Finished(operationDefinition.DeserializeResult(result)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(executionResult),
                executionResult,
                "Unknow operation execution result type"
            ),
        };
    }

    public static SerializedOperationArgs Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        TArgs args
    )
    {
        try
        {
            var serializedArgsValue = operationDefinition.SerializeArgs(args);
            return new SerializedOperationArgs(serializedArgsValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize args of operation {operationDefinition.Discriminator}",
                e
            );
        }
    }

    public static TArgs Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperationArgs serializedArgs
    )
    {
        try
        {
            return operationDefinition.DeserializeArgs(serializedArgs.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize args of operation {operationDefinition.Discriminator}",
                e
            );
        }
    }
}
