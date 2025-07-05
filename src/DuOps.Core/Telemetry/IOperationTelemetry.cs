using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
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
        IInterResultDefinition resultDefinition,
        string? interResultKey,
        string serializedResult
    );

    void OnOperationFinished(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        string serializedResult
    );

    void OnOperationThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Exception exception
    );

    void OnInterResultThrewException(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        IInterResultDefinition interResultDefinition,
        string? interResultKey,
        Exception exception
    );

    void OnOperationYielded(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        string yieldReason,
        string yieldReasonMessage
    );
}
