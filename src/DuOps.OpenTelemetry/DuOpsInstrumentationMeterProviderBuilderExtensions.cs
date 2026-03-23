using DuOps.Core.DependencyInjection;
using OpenTelemetry.Metrics;

namespace DuOps.OpenTelemetry;

public static class DuOpsInstrumentationMeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddDuOpsInstrumentation(this MeterProviderBuilder builder) =>
        builder.AddMeter("DuOps.Operation");

    public static DuOpsBuilder AddOpenTelemetryInstrumentation(this DuOpsBuilder builder)
    {
        builder.Services.ConfigureOpenTelemetryMeterProvider(meterProviderBuilder =>
            meterProviderBuilder.AddDuOpsInstrumentation()
        );

        return builder;
    }
}
