using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationManagers;
using DuOps.Core.OperationPollers;
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
        Action<DuOpsOptionsBuilder> configure
    )
    {
        services.TryAddScoped<IOperationManager, OperationManager>();
        services.TryAddScoped<IOperationPoller, OperationPoller>();

        services.TryAddSingleton<IOperationDefinitionRegistry, OperationDefinitionRegistry>();

        services.TryAddSingleton<IOperationTelemetry, OperationTelemetry>();
        services.TryAddSingleton<IOperationMetrics, OperationMetrics>();

        var builder = new DuOpsOptionsBuilder(services);
        configure(builder);

        return services;
    }

    public static IServiceCollection AddDuOpsOperation<TArgs, TResult, TImplementation>(
        this IServiceCollection services,
        IOperationDefinition<TArgs, TResult> definition
    )
        where TImplementation : class, IOperationImplementation<TArgs, TResult>
    {
        services.TryAddSingleton(definition);
        services.TryAddScoped<IOperationImplementation<TArgs, TResult>, TImplementation>();
        services.TryAddSingleton<
            IOperationDefinitionRegistryItem,
            OperationDefinitionRegistryItem<TArgs, TResult>
        >();

        return services;
    }
}
