using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Telemetry.Metrics;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.Telemetry;

internal sealed class OperationTelemetry(
    ILogger<OperationTelemetry> logger,
    IOperationMetrics metrics
) : IOperationTelemetry
{
    public void OnInnerResultAdded(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        SerializedInnerResult innerResult
    )
    {
        if (innerResult.Id is null)
        {
            logger.LogInnerResultAdded(
                operationDefinition.Type,
                serializedOperationId,
                innerResult.Type,
                innerResult.Value
            );
        }
        else
        {
            logger.LogInnerResultAdded(
                operationDefinition.Type,
                serializedOperationId,
                innerResult.Type,
                innerResult.Id.Value,
                innerResult.Value
            );
        }
        metrics.OnInnerResultAdded(operationDefinition.Type, innerResult.Type);
    }

    public void OnOperationThrewException(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        Exception exception,
        DateTimeOffset retryingAt
    )
    {
        logger.LogOperationThrewException(
            exception,
            operationDefinition.Type,
            serializedOperationId,
            retryingAt
        );

        metrics.OnOperationThrewException(operationDefinition.Type, exception);
    }

    public void OnOperationFailed(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        Exception exception
    )
    {
        logger.LogOperationFailed(exception, operationDefinition.Type, serializedOperationId);
        metrics.OnOperationThrewException(operationDefinition.Type, exception);
        metrics.OnOperationFailed(operationDefinition.Type);
    }

    public void OnOperationWaiting(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        DateTimeOffset waitingUntil,
        string reason
    )
    {
        logger.LogOperationWaiting(
            operationDefinition.Type,
            serializedOperationId,
            waitingUntil,
            reason
        );
        metrics.OnOperationWaiting(operationDefinition.Type, reason);
    }

    public void OnInnerResultThrewException(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        IInnerResultDefinition innerResultDefinition,
        SerializedInnerResultId? innerResultKey,
        Exception exception
    )
    {
        if (innerResultKey is null)
        {
            logger.LogInnerResultThrewException(
                exception,
                operationDefinition.Type,
                serializedOperationId,
                innerResultDefinition.Type
            );
        }
        else
        {
            logger.LogInnerResultThrewException(
                exception,
                operationDefinition.Type,
                serializedOperationId,
                innerResultDefinition.Type,
                innerResultKey.Value
            );
        }
        metrics.OnInnerResultThrewException(
            operationDefinition.Type,
            innerResultDefinition.Type,
            exception
        );
    }

    public void OnOperationYielded(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId
    )
    {
        logger.LogOperationYielded(operationDefinition.Type, serializedOperationId);

        metrics.OnOperationYielded(operationDefinition.Type);
    }

    public void OnOperationFinished(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        SerializedOperationResult serializedResult
    )
    {
        logger.LogOperationFinished(
            operationDefinition.Type,
            serializedOperationId,
            serializedResult
        );

        metrics.OnOperationFinished(operationDefinition.Type);
    }
}
