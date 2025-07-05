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
            new OperationArgs<TArgs>(args),
            OperationState<TResult>.Yielded.Instance
        );
    }

    #region OperationArgs

    public static SerializedOperationArgs Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationArgs<TArgs> args
    )
    {
        try
        {
            var serializedArgsValue = operationDefinition.SerializeArgs(args.Value);
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

    public static OperationArgs<TArgs> Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperationArgs serializedArgs
    )
    {
        try
        {
            var argsValue = operationDefinition.DeserializeArgs(serializedArgs.Value);

            return new OperationArgs<TArgs>(argsValue);
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

    #region OperationResult

    public static SerializedOperationResult Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationResult<TResult> operationResult
    )
    {
        try
        {
            var serializedResultValue = operationDefinition.SerializeResult(operationResult.Value);
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

    public static OperationResult<TResult> Deserialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperationResult serializedResult
    )
    {
        try
        {
            var resultValue = operationDefinition.DeserializeResult(serializedResult.Value);
            return new OperationResult<TResult>(resultValue);
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

    #region OperationState

    public static SerializedOperationState Serialize<TArgs, TResult>(
        this IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationState<TResult> state
    )
    {
        return state switch
        {
            OperationState<TResult>.Yielded => SerializedOperationState.Yielded.Instance,
            OperationState<TResult>.Finished { Result: var result } =>
                new SerializedOperationState.Finished(operationDefinition.Serialize(result)),
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
                new OperationState<TResult>.Finished(operationDefinition.Deserialize(result)),
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
            operationDefinition.Serialize(operation.Args),
            operationDefinition.Serialize(operation.State)
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
            operationDefinition.Deserialize(serializedOperation.Args),
            operationDefinition.Deserialize(serializedOperation.State)
        );
    }

    #endregion
}
