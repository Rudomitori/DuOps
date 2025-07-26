using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Telemetry.Metrics;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.Telemetry;

internal sealed class OperationTelemetry(
    ILogger<OperationTelemetry> logger,
    IOperationMetrics metrics
) : IOperationTelemetry
{
    public void OnOperationStartedInBackground(
        IOperationDefinition operationDefinition,
        SerializedOperation operation
    )
    {
        logger.LogOperationStartedInBackground(
            operationDefinition.Discriminator,
            operation.Id,
            operation.PollingScheduleId,
            operation.Args
        );
        metrics.OnOperationStarted(operation.Discriminator);
    }

    public void OnInterResultAdded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        SerializedInterResult interResult
    )
    {
        if (interResult.Key is null)
        {
            logger.LogInterResultAdded(
                operationDefinition.Discriminator,
                operationId,
                interResult.Discriminator,
                interResult.Value
            );
        }
        else
        {
            logger.LogInterResultAdded(
                operationDefinition.Discriminator,
                operationId,
                interResult.Discriminator,
                interResult.Key.Value,
                interResult.Value
            );
        }
        metrics.OnInterResultAdded(operationDefinition.Discriminator, interResult.Discriminator);
    }

    public void OnOperationThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Exception exception
    )
    {
        logger.LogOperationThrewException(
            exception,
            operationDefinition.Discriminator,
            operationId
        );
        metrics.OnOperationThrewException(operationDefinition.Discriminator, exception);
    }

    public void OnInterResultThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        IInterResultDefinition interResultDefinition,
        SerializedInterResultKey? interResultKey,
        Exception exception
    )
    {
        if (interResultKey is null)
        {
            logger.LogInterResultThrewException(
                exception,
                operationDefinition.Discriminator,
                operationId,
                interResultDefinition.Discriminator
            );
        }
        else
        {
            logger.LogInterResultThrewException(
                exception,
                operationDefinition.Discriminator,
                operationId,
                interResultDefinition.Discriminator,
                interResultKey.Value
            );
        }
        metrics.OnInterResultThrewException(
            operationDefinition.Discriminator,
            interResultDefinition.Discriminator,
            exception
        );
    }

    public void OnOperationYielded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        string yieldReason,
        string? yieldReasonMessage
    )
    {
        logger.LogOperationYielded(
            operationDefinition.Discriminator,
            operationId,
            yieldReason,
            yieldReasonMessage ?? ""
        );

        metrics.OnOperationYielded(operationDefinition.Discriminator, yieldReason);
    }

    public void OnOperationFinished(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        SerializedOperationResult serializedResult
    )
    {
        logger.LogOperationFinished(
            operationDefinition.Discriminator,
            operationId,
            serializedResult
        );

        metrics.OnOperationFinished(operationDefinition.Discriminator);
    }
}
