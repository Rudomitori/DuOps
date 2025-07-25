using DuOps.Core.DependencyInjection;
using OpenTelemetry.Metrics;

namespace DuOps.OpenTelemetry;

public static class DuOpsInstrumentationMeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddDuOpsInstrumentation(this MeterProviderBuilder builder) =>
        builder.AddMeter("DuOps.Operation");

    public static DuOpsOptionsBuilder AddOpenTelemetryInstrumentation(
        this DuOpsOptionsBuilder builder
    )
    {
        builder.Services.ConfigureOpenTelemetryMeterProvider(meterProviderBuilder =>
            meterProviderBuilder.AddDuOpsInstrumentation()
        );

        return builder;
    }
}
