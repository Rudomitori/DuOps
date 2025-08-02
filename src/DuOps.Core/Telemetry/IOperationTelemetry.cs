using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Telemetry;

public interface IOperationTelemetry
{
    void OnOperationStartedInBackground(
        IOperationDefinition operationDefinition,
        SerializedOperation operation
    );

    void OnInterResultAdded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        SerializedInterResult interResult
    );

    void OnOperationFinished(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        SerializedOperationResult serializedResult
    );

    void OnOperationThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Exception exception,
        DateTimeOffset retryingAt
    );

    void OnOperationFailed(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Exception exception
    );

    void OnOperationWaiting(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        DateTimeOffset waitingUntil,
        string reason
    );

    void OnInterResultThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        IInterResultDefinition interResultDefinition,
        SerializedInterResultKey? interResultKey,
        Exception exception
    );

    void OnOperationYielded(IOperationDefinition operationDefinition, OperationId operationId);
}
