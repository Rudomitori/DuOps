using DuOps.Core.Registry;
using DuOps.Core.Telemetry;
using DuOps.Core.Telemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuOps.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDuOps(
        this IServiceCollection services,
        Action<DuOpsBuilder> configure
    )
    {
        services.TryAddSingleton(TimeProvider.System);

        var registry = new OperationRegistry();

        services.AddSingleton(registry);

        services.AddSingleton<IOperationTelemetry, OperationTelemetry>();
        services.AddSingleton<IOperationMetrics, OperationMetrics>();

        var builder = new DuOpsBuilder(services, registry);
        configure(builder);

        return services;
    }
}
