using System.Diagnostics.Metrics;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace DuOps.Core.Telemetry.Metrics;

internal sealed partial class OperationMetrics : IOperationMetrics
{
    private readonly OperationStartedCounter _operationStartedCounter;
    private readonly InterResultAddedCounter _interResultAddedCounter;
    private readonly OperationThrewExceptionCounter _operationThrewExceptionCounter;
    private readonly InterResultThrewExceptionCounter _interResultThrewExceptionCounter;
    private readonly OperationYieldedCounter _operationYieldedCounter;
    private readonly OperationFinishedCounter _operationFinishedCounter;

    public OperationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DuOps.Operation");
        _operationStartedCounter = CreateOperationStartedCounter(meter);
        _interResultAddedCounter = CreateInterResultAddedCounter(meter);
        _operationThrewExceptionCounter = CreateOperationThrewExceptionCounter(meter);
        _interResultThrewExceptionCounter = CreateInterResultThrewExceptionCounter(meter);
        _operationYieldedCounter = CreateOperationYieldedCounter(meter);
        _operationFinishedCounter = CreateOperationFinishedCounter(meter);
    }

    public void OnOperationStarted(OperationDiscriminator discriminator)
    {
        _operationStartedCounter.Add(1, new OperationStartedCounterTags(discriminator));
    }

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

    public void OnOperationYielded(OperationDiscriminator discriminator, string reason)
    {
        _operationYieldedCounter.Add(1, new OperationYieldedCounterTags(discriminator, reason));
    }

    public void OnOperationFinished(OperationDiscriminator discriminator)
    {
        _operationFinishedCounter.Add(1, new OperationFinishedCounterTags(discriminator));
    }

    public readonly record struct OperationFinishedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationFinishedCounterTags), Name = "duops.operation.finished")]
    public static partial OperationFinishedCounter CreateOperationFinishedCounter(Meter meter);

    public readonly record struct OperationYieldedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("yield.reason")] string Reason
    );

    [Counter<int>(typeof(OperationYieldedCounterTags), Name = "duops.operation.yielded")]
    public static partial OperationYieldedCounter CreateOperationYieldedCounter(Meter meter);

    public readonly record struct InterResultThrewExceptionCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("inter_result.discriminator")] string InterResultDiscriminator,
        [property: TagName("exception.type.name")] string ExceptionType
    );

    [Counter<int>(
        typeof(InterResultThrewExceptionCounterTags),
        Name = "duops.operation.inter_result.threw_exception"
    )]
    public static partial InterResultThrewExceptionCounter CreateInterResultThrewExceptionCounter(
        Meter meter
    );

    public readonly record struct OperationThrewExceptionCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("exception.type.name")] string ExceptionType
    );

    [Counter<int>(
        typeof(OperationThrewExceptionCounterTags),
        Name = "duops.operation.threw_exception"
    )]
    public static partial OperationThrewExceptionCounter CreateOperationThrewExceptionCounter(
        Meter meter
    );

    public readonly record struct InterResultAddedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator,
        [property: TagName("inter_result.discriminator")] string InterResultDiscriminator
    );

    [Counter<int>(typeof(InterResultAddedCounterTags), Name = "duops.operation.inter_result.added")]
    public static partial InterResultAddedCounter CreateInterResultAddedCounter(Meter meter);

    public readonly record struct OperationStartedCounterTags(
        [property: TagName("operation.discriminator")] string OperationDiscriminator
    );

    [Counter<int>(typeof(OperationStartedCounterTags), Name = "duops.operation.started")]
    public static partial OperationStartedCounter CreateOperationStartedCounter(Meter meter);
}
