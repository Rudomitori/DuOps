using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults.Definitions;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.Telemetry;

internal sealed class OperationTelemetry(ILogger<OperationTelemetry> logger) : IOperationTelemetry
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
    }

    public void OnInterResultAdded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        IInterResultDefinition resultDefinition,
        string? interResultKey,
        string serializedResult
    )
    {
        if (interResultKey is null)
        {
            logger.LogInterResultAdded(
                operationDefinition.Discriminator,
                operationId,
                resultDefinition.Discriminator,
                serializedResult
            );
        }
        else
        {
            logger.LogInterResultAdded(
                operationDefinition.Discriminator,
                operationId,
                resultDefinition.Discriminator,
                interResultKey,
                serializedResult
            );
        }
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
    }

    public void OnInterResultThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        IInterResultDefinition interResultDefinition,
        string? interResultKey,
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
                interResultKey
            );
        }
    }

    public void OnOperationYielded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        string yieldReason,
        string yieldReasonMessage
    )
    {
        logger.LogOperationYielded(
            operationDefinition.Discriminator,
            operationId,
            yieldReason,
            yieldReasonMessage
        );
    }

    public void OnOperationFinished(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        string serializedResult
    )
    {
        logger.LogOperationFinished(
            operationDefinition.Discriminator,
            operationId,
            serializedResult
        );
    }
}
