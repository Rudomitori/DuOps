using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationManagers;
using DuOps.Core.OperationPollers;
using DuOps.Core.Registry;
using DuOps.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuOps.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOperationManager(
        this IServiceCollection services
    )
    {
        return services.AddScoped<IOperationManager, OperationManager>();
    }

    public static IServiceCollection AddOperationPoller(
        this IServiceCollection services
    )
    {
        return services.AddScoped<IOperationPoller, OperationPoller>();
    }

    public static IServiceCollection AddOperationTelemetry(
        this IServiceCollection services
    )
    {
        return services.AddScoped<IOperationTelemetry, OperationTelemetry>();
    }

    public static IServiceCollection AddOperationDefinitionRegistry(
        this IServiceCollection services
    )
    {
        return services.AddScoped<IOperationDefinitionRegistry, OperationDefinitionRegistry>();
    }

    public static IServiceCollection RegisterOperationDefinition<TArgs, TResult, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, IOperationDefinition<TArgs, TResult>> factory
    )
        where TImplementation : class, IOperationImplementation<TArgs, TResult>
    {
        services.TryAddScoped(factory);
        services.TryAddScoped<IOperationImplementation<TArgs, TResult>, TImplementation>();
        services.AddScoped<IOperationDefinitionRegistryItem, OperationDefinitionRegistryItem<TArgs, TResult>>();

        return services;
    }
}
