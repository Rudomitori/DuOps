using DuOps.Core.Exceptions;
using DuOps.Core.Operations;

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
            args,
            OperationState<TResult>.Yielded.Instance,
            []
        );
    }

    #region Args serialization

    public static SerializedOperationArgs SerializeArgsAndWrapException<TArgs, TResult>(
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

    public static TArgs DeserializeArgsAndWrapException<TArgs, TResult>(
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

    #endregion

    #region Result serialization

    public static SerializedOperationResult SerializeResultAndWrapException<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        TResult operationResult
    )
    {
        try
        {
            var serializedResultValue = operationDefinition.SerializeResult(operationResult);
            return new SerializedOperationResult(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize result of operation {operationDefinition.Discriminator}",
                e
            );
        }
    }

    public static TResult DeserializeResultAndWrapException<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperationResult serializedResult
    )
    {
        try
        {
            return operationDefinition.DeserializeResult(serializedResult.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize result of operation {operationDefinition.Discriminator}",
                e
            );
        }
    }

    #endregion

    #region OperationState serialization

    public static SerializedOperationState Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationState<TResult> state
    )
    {
        return state switch
        {
            OperationState<TResult>.Yielded => SerializedOperationState.Yielded.Instance,
            OperationState<TResult>.Finished { Result: var result } =>
                new SerializedOperationState.Finished(
                    operationDefinition.SerializeResultAndWrapException(result)
                ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknow operation execution result type"
            ),
        };
    }

    public static OperationState<TResult> Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperationState state
    )
    {
        return state switch
        {
            SerializedOperationState.Yielded => OperationState<TResult>.Yielded.Instance,
            SerializedOperationState.Finished { Result: var result } =>
                new OperationState<TResult>.Finished(
                    operationDefinition.DeserializeResultAndWrapException(result)
                ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknow operation execution result type"
            ),
        };
    }

    #endregion

    #region Operation

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
            operationDefinition.SerializeArgsAndWrapException(operation.Args),
            operationDefinition.Serialize(operation.State),
            operation.SerializedInterResults
        );
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
            operationDefinition.DeserializeArgsAndWrapException(serializedOperation.Args),
            operationDefinition.Deserialize(serializedOperation.State),
            serializedOperation.InterResults
        );
    }

    #endregion
}
