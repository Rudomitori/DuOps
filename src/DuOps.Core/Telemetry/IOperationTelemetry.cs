using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.Telemetry;

public interface IOperationTelemetry
{
    void OnInnerResultAdded(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        SerializedInnerResult innerResult
    );

    void OnOperationFinished(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        SerializedOperationResult serializedResult
    );

    void OnOperationThrewException(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        Exception exception,
        DateTimeOffset retryingAt
    );

    void OnOperationFailed(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        Exception exception
    );

    void OnOperationWaiting(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        DateTimeOffset waitingUntil,
        string reason
    );

    void OnInnerResultThrewException(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId,
        IInnerResultDefinition innerResultDefinition,
        SerializedInnerResultId? innerResultKey,
        Exception exception
    );

    void OnOperationYielded(
        IOperationDefinition operationDefinition,
        SerializedOperationId serializedOperationId
    );
}
