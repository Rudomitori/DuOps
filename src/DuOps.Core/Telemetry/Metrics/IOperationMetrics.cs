using DuOps.Core.OperationDefinitions;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Telemetry.Metrics;

public interface IOperationMetrics
{
    void OnOperationStarted(OperationType type);

    void OnInnerResultAdded(OperationType operationType, InnerResultType innerResulttype);

    void OnOperationThrewException(OperationType operationType, Exception exception);

    void OnOperationWaiting(OperationType operationType, string reason);

    void OnOperationFailed(OperationType operationType);

    void OnInnerResultThrewException(
        OperationType operationType,
        InnerResultType innerResulttype,
        Exception exception
    );

    void OnOperationYielded(OperationType type);
    void OnOperationFinished(OperationType type);
}
