using System.Diagnostics.Metrics;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace DuOps.Core.Telemetry.Metrics;

internal sealed partial class OperationMetrics : IOperationMetrics
{
    private readonly OperationStartedCounter _operationStartedCounter;
    private readonly InterResultAddedCounter _interResultAddedCounter;
    private readonly InterResultThrewExceptionCounter _interResultThrewExceptionCounter;
    private readonly OperationThrewExceptionCounter _operationThrewExceptionCounter;
    private readonly OperationWaitingsCounter _operationWaitingsCounter;
    private readonly OperationYieldedCounter _operationYieldedCounter;
    private readonly OperationFinishedCounter _operationFinishedCounter;
    private readonly OperationFailedCounterCounter _operationFailedCounter;

    public OperationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DuOps.Operation");
        _operationStartedCounter = CreateOperationStartedCounter(meter);
        _interResultAddedCounter = CreateInterResultAddedCounter(meter);
        _interResultThrewExceptionCounter = CreateInterResultThrewExceptionCounter(meter);
        _operationThrewExceptionCounter = CreateOperationThrewExceptionCounter(meter);
        _operationWaitingsCounter = CreateOperationWaitingsCounter(meter);
        _operationYieldedCounter = CreateOperationYieldedCounter(meter);
        _operationFinishedCounter = CreateOperationFinishedCounter(meter);
        _operationFailedCounter = CreateOperationFailedCounter(meter);
    }

    #region OperationStarted

    public void OnOperationStarted(OperationDiscriminator discriminator)
    {
        _operationStartedCounter.Add(1, new OperationStartedCounterTags(discriminator));
    }

    internal readonly record struct OperationStartedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationStartedCounterTags), Name = "duops.operation.started")]
    internal static partial OperationStartedCounter CreateOperationStartedCounter(Meter meter);

    #endregion

    #region InterResultAdded

    public void OnInterResultAdded(
        OperationDiscriminator operationDiscriminator,
        InterResultDiscriminator interResultDiscriminator
    )
    {
        _interResultAddedCounter.Add(
            1,
            new InterResultAddedCounterTags(operationDiscriminator, interResultDiscriminator)
        );
    }

    internal readonly record struct InterResultAddedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("inter_result.discriminator")] string InterResultDiscriminator
    );

    [Counter<int>(typeof(InterResultAddedCounterTags), Name = "duops.operation.inter_result.added")]
    internal static partial InterResultAddedCounter CreateInterResultAddedCounter(Meter meter);

    #endregion

    #region InterResultThrewException

    public void OnInterResultThrewException(
        OperationDiscriminator operationDiscriminator,
        InterResultDiscriminator interResultDiscriminator,
        Exception exception
    )
    {
        // TODO: Handle AggregateException
        var exceptionTypeName = exception.GetType().Name;
        _interResultThrewExceptionCounter.Add(
            1,
            new InterResultThrewExceptionCounterTags(
                operationDiscriminator,
                interResultDiscriminator,
                exceptionTypeName
            )
        );
    }

    internal readonly record struct InterResultThrewExceptionCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("inter_result.discriminator")] string InterResultDiscriminator,
        [property: TagName("exception.type.name")] string ExceptionType
    );

    [Counter<int>(
        typeof(InterResultThrewExceptionCounterTags),
        Name = "duops.operation.inter_result.threw_exception"
    )]
    internal static partial InterResultThrewExceptionCounter CreateInterResultThrewExceptionCounter(
        Meter meter
    );

    #endregion

    #region OperationWaitings

    public void OnOperationWaiting(OperationDiscriminator operationDiscriminator, string reason)
    {
        _operationWaitingsCounter.Add(
            1,
            new OperationWaitingsCounterTags(operationDiscriminator, reason)
        );
    }

    internal readonly record struct OperationWaitingsCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("waiting.reason")] string WaitingReason
    );

    [Counter<int>(typeof(OperationWaitingsCounterTags), Name = "duops.operation.waitings")]
    internal static partial OperationWaitingsCounter CreateOperationWaitingsCounter(Meter meter);

    #endregion

    #region OperationYielded

    public void OnOperationYielded(OperationDiscriminator discriminator)
    {
        _operationYieldedCounter.Add(1, new OperationYieldedCounterTags(discriminator));
    }

    internal readonly record struct OperationYieldedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationYieldedCounterTags), Name = "duops.operation.yielded")]
    internal static partial OperationYieldedCounter CreateOperationYieldedCounter(Meter meter);

    #endregion

    #region OperationThrewException

    public void OnOperationThrewException(
        OperationDiscriminator operationDiscriminator,
        Exception exception
    )
    {
        // TODO: Handle AggregateException
        var exceptionTypeName = exception.GetType().Name;
        _operationThrewExceptionCounter.Add(
            1,
            new OperationThrewExceptionCounterTags(operationDiscriminator, exceptionTypeName)
        );
    }

    internal readonly record struct OperationThrewExceptionCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
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

    public void OnOperationFinished(OperationDiscriminator discriminator)
    {
        _operationFinishedCounter.Add(1, new OperationFinishedCounterTags(discriminator));
    }

    internal readonly record struct OperationFinishedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationFinishedCounterTags), Name = "duops.operation.finished")]
    internal static partial OperationFinishedCounter CreateOperationFinishedCounter(Meter meter);

    #endregion

    #region OperationFailed

    public void OnOperationFailed(OperationDiscriminator operationDiscriminator)
    {
        _operationFailedCounter.Add(1, new OperationFailedCounterTags(operationDiscriminator));
    }

    internal readonly record struct OperationFailedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationFailedCounterTags), Name = "duops.operation.failed")]
    internal static partial OperationFailedCounterCounter CreateOperationFailedCounter(Meter meter);

    #endregion
}
