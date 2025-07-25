using System.Diagnostics.Metrics;
using DuOps.Core.Telemetry.Metrics;
using DuOps.Core.Tests.TestOperation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Shouldly;

namespace DuOps.Core.Tests;

public sealed class OperationMetricsTests
{
    private ServiceProvider _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSingleton<IOperationMetrics, OperationMetrics>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    [Test]
    public void OnOperationStarted()
    {
        var meterFactory = _serviceProvider.GetRequiredService<IMeterFactory>();
        var operationMetrics = _serviceProvider.GetRequiredService<IOperationMetrics>();

        var metricCollector = new MetricCollector<int>(
            meterFactory,
            "DuOps.Operation",
            "duops.operation.started"
        );

        operationMetrics.OnOperationStarted(TestOperationDefinition.Instance.Discriminator);

        var measurements = metricCollector.GetMeasurementSnapshot();
        var measurement = measurements.ShouldHaveSingleItem();
        var tag = measurement.Tags.ShouldHaveSingleItem();
        tag.Key.ShouldBe("operation.discriminator");
        tag.Value.ShouldBe(TestOperationDefinition.Instance.Discriminator.Value);
    }
}
