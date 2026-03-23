using DuOps.Core.Exceptions;
using DuOps.Core.Operations;

namespace DuOps.Core.OperationDefinitions;

public static class OperationDefinitionExtensions
{
    #region Id serialization

    public static SerializedOperationId SerializeId<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        TId operationId
    )
    {
        try
        {
            var serializedArgsValue = operationDefinition.IdSerializer.Serialize(operationId);
            return new SerializedOperationId(serializedArgsValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize id of operation {operationDefinition.Type}",
                e
            );
        }
    }

    public static TId DeserializeId<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        SerializedOperationId serializedOperationId
    )
    {
        try
        {
            return operationDefinition.IdSerializer.Deserialize(serializedOperationId.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize id of operation {operationDefinition.Type}",
                e
            );
        }
    }

    #endregion

    #region Args serialization

    public static SerializedOperationArgs SerializeArgs<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        TArgs args
    )
    {
        try
        {
            var serializedArgsValue = operationDefinition.ArgsSerializer.Serialize(args);
            return new SerializedOperationArgs(serializedArgsValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize args of operation {operationDefinition.Type}",
                e
            );
        }
    }

    public static TArgs DeserializeArgs<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        SerializedOperationArgs serializedArgs
    )
    {
        try
        {
            return operationDefinition.ArgsSerializer.Deserialize(serializedArgs.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize args of operation {operationDefinition.Type}",
                e
            );
        }
    }

    #endregion

    #region Result serialization

    public static SerializedOperationResult SerializeResult<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        TResult operationResult
    )
    {
        try
        {
            var serializedResultValue = operationDefinition.ResultSerializer.Serialize(
                operationResult
            );
            return new SerializedOperationResult(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize result of operation {operationDefinition.Type}",
                e
            );
        }
    }

    public static TResult DeserializeResult<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        SerializedOperationResult serializedResult
    )
    {
        try
        {
            return operationDefinition.ResultSerializer.Deserialize(serializedResult.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize result of operation {operationDefinition.Type}",
                e
            );
        }
    }

    #endregion

    #region OperationState serialization

    public static SerializedOperationState Serialize<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        OperationState<TResult> state
    )
    {
        return state switch
        {
            OperationState<TResult>.Active => SerializedOperationState.Active.Instance,
            OperationState<TResult>.Competed competed => new SerializedOperationState.Completed(
                competed.At,
                operationDefinition.SerializeResult(competed.Result)
            ),
            OperationState<TResult>.Failed failed => new SerializedOperationState.Failed(
                failed.At,
                failed.Reason
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknow operation state"
            ),
        };
    }

    public static OperationState<TResult> Deserialize<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        SerializedOperationState state
    )
    {
        return state switch
        {
            SerializedOperationState.Active => OperationState<TResult>.Active.Instance,
            SerializedOperationState.Completed completed => new OperationState<TResult>.Competed(
                completed.At,
                operationDefinition.DeserializeResult(completed.Result)
            ),
            SerializedOperationState.Failed failed => new OperationState<TResult>.Failed(
                failed.At,
                failed.Reason
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknow operation state"
            ),
        };
    }

    #endregion

    #region Operation

    public static SerializedOperation Serialize<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        Operation<TId, TArgs, TResult> operation
    )
    {
        return new SerializedOperation(
            operationDefinition.Type,
            operationDefinition.SerializeId(operation.Id),
            QueueId: operation.QueueId,
            ScheduledAt: operation.ScheduledAt,
            operationDefinition.SerializeArgs(operation.Args),
            CreatedAt: operation.CreatedAt,
            operationDefinition.Serialize(operation.State),
            RetryCount: operation.RetryCount
        );
    }

    public static Operation<TId, TArgs, TResult> Deserialize<TId, TArgs, TResult>(
        this IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation
    )
    {
        return new Operation<TId, TArgs, TResult>(
            operationDefinition.Type,
            operationDefinition.DeserializeId(serializedOperation.Id),
            QueueId: serializedOperation.QueueId,
            ScheduledAt: serializedOperation.ScheduledAt,
            operationDefinition.DeserializeArgs(serializedOperation.Args),
            CreatedAt: serializedOperation.CreatedAt,
            operationDefinition.Deserialize(serializedOperation.State),
            RetryCount: serializedOperation.RetryCount
        );
    }

    #endregion
}
