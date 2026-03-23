using System.Diagnostics.Metrics;
using DuOps.Core.OperationDefinitions;
using Microsoft.Extensions.Diagnostics.Metrics;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Telemetry.Metrics;

internal sealed partial class OperationMetrics : IOperationMetrics
{
    private readonly OperationStartedCounter _operationStartedCounter;
    private readonly InnerResultAddedCounter _innerResultAddedCounter;
    private readonly InnerResultThrewExceptionCounter _innerResultThrewExceptionCounter;
    private readonly OperationThrewExceptionCounter _operationThrewExceptionCounter;
    private readonly OperationWaitingsCounter _operationWaitingsCounter;
    private readonly OperationYieldedCounter _operationYieldedCounter;
    private readonly OperationFinishedCounter _operationFinishedCounter;
    private readonly OperationFailedCounterCounter _operationFailedCounter;

    public OperationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DuOps.Operation");
        _operationStartedCounter = CreateOperationStartedCounter(meter);
        _innerResultAddedCounter = CreateInnerResultAddedCounter(meter);
        _innerResultThrewExceptionCounter = CreateInnerResultThrewExceptionCounter(meter);
        _operationThrewExceptionCounter = CreateOperationThrewExceptionCounter(meter);
        _operationWaitingsCounter = CreateOperationWaitingsCounter(meter);
        _operationYieldedCounter = CreateOperationYieldedCounter(meter);
        _operationFinishedCounter = CreateOperationFinishedCounter(meter);
        _operationFailedCounter = CreateOperationFailedCounter(meter);
    }

    #region OperationStarted

    public void OnOperationStarted(OperationType type)
    {
        _operationStartedCounter.Add(1, new OperationStartedCounterTags(type));
    }

    internal readonly record struct OperationStartedCounterTags(
        [property: TagName("operation.type")] string Operationtype
    );

    [Counter<int>(typeof(OperationStartedCounterTags), Name = "duops.operation.started")]
    internal static partial OperationStartedCounter CreateOperationStartedCounter(Meter meter);

    #endregion

    #region InnerResultAdded

    public void OnInnerResultAdded(OperationType operationType, InnerResultType innerResulttype)
    {
        _innerResultAddedCounter.Add(
            1,
            new InnerResultAddedCounterTags(operationType, innerResulttype)
        );
    }

    internal readonly record struct InnerResultAddedCounterTags(
        [property: TagName("operation.type")] string Operationtype,
        [property: TagName("inner_result.type")] string InnerResulttype
    );

    [Counter<int>(typeof(InnerResultAddedCounterTags), Name = "duops.operation.inner_result.added")]
    internal static partial InnerResultAddedCounter CreateInnerResultAddedCounter(Meter meter);

    #endregion

    #region InnerResultThrewException

    public void OnInnerResultThrewException(
        OperationType operationType,
        InnerResultType innerResulttype,
        Exception exception
    )
    {
        // TODO: Handle AggregateException
        var exceptionTypeName = exception.GetType().Name;
        _innerResultThrewExceptionCounter.Add(
            1,
            new InnerResultThrewExceptionCounterTags(
                operationType,
                innerResulttype,
                exceptionTypeName
            )
        );
    }

    internal readonly record struct InnerResultThrewExceptionCounterTags(
        [property: TagName("operation.type")] string Operationtype,
        [property: TagName("inner_result.type")] string InnerResulttype,
        [property: TagName("exception.type.name")] string ExceptionType
    );

    [Counter<int>(
        typeof(InnerResultThrewExceptionCounterTags),
        Name = "duops.operation.inner_result.threw_exception"
    )]
    internal static partial InnerResultThrewExceptionCounter CreateInnerResultThrewExceptionCounter(
        Meter meter
    );

    #endregion

    #region OperationWaitings

    public void OnOperationWaiting(OperationType operationType, string reason)
    {
        _operationWaitingsCounter.Add(1, new OperationWaitingsCounterTags(operationType, reason));
    }

    internal readonly record struct OperationWaitingsCounterTags(
        [property: TagName("operation.type")] string Operationtype,
        [property: TagName("waiting.reason")] string WaitingReason
    );

    [Counter<int>(typeof(OperationWaitingsCounterTags), Name = "duops.operation.waitings")]
    internal static partial OperationWaitingsCounter CreateOperationWaitingsCounter(Meter meter);

    #endregion

    #region OperationYielded

    public void OnOperationYielded(OperationType type)
    {
        _operationYieldedCounter.Add(1, new OperationYieldedCounterTags(type));
    }

    internal readonly record struct OperationYieldedCounterTags(
        [property: TagName("operation.type")] string Operationtype
    );

    [Counter<int>(typeof(OperationYieldedCounterTags), Name = "duops.operation.yielded")]
    internal static partial OperationYieldedCounter CreateOperationYieldedCounter(Meter meter);

    #endregion

    #region OperationThrewException

    public void OnOperationThrewException(OperationType operationType, Exception exception)
    {
        // TODO: Handle AggregateException
        var exceptionTypeName = exception.GetType().Name;
        _operationThrewExceptionCounter.Add(
            1,
            new OperationThrewExceptionCounterTags(operationType, exceptionTypeName)
        );
    }

    internal readonly record struct OperationThrewExceptionCounterTags(
        [property: TagName("operation.type")] string Operationtype,
        [property: TagName("exception.type.name")] string ExceptionType
    );

    [Counter<int>(
        typeof(OperationThrewExceptionCounterTags),
        Name = "duops.operation.threw_exception"
    )]
    internal static partial OperationThrewExceptionCounter CreateOperationThrewExceptionCounter(
        Meter meter
    );

    #endregion

    #region OperationFinished

    public void OnOperationFinished(OperationType type)
    {
        _operationFinishedCounter.Add(1, new OperationFinishedCounterTags(type));
    }

    internal readonly record struct OperationFinishedCounterTags(
        [property: TagName("operation.type")] string Operationtype
    );

    [Counter<int>(typeof(OperationFinishedCounterTags), Name = "duops.operation.finished")]
    internal static partial OperationFinishedCounter CreateOperationFinishedCounter(Meter meter);

    #endregion

    #region OperationFailed

    public void OnOperationFailed(OperationType operationType)
    {
        _operationFailedCounter.Add(1, new OperationFailedCounterTags(operationType));
    }

    internal readonly record struct OperationFailedCounterTags(
        [property: TagName("operation.type")] string Operationtype
    );

    [Counter<int>(typeof(OperationFailedCounterTags), Name = "duops.operation.failed")]
    internal static partial OperationFailedCounterCounter CreateOperationFailedCounter(Meter meter);

    #endregion
}
