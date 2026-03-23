using DuOps.Core.OperationDefinitions;
using DuOps.Core.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.DependencyInjection;

public sealed class DuOpsBuilder
{
    public IServiceCollection Services { get; }
    internal OperationRegistry Registry { get; }

    internal DuOpsBuilder(IServiceCollection services, OperationRegistry registry)
    {
        Services = services;
        Registry = registry;
    }

    public void AddOperation<TId, TArgs, TResult>(
        IOperationDefinition<TId, TArgs, TResult> operationDefinition,
        Action<OperationBuilder<TId, TArgs, TResult>> configure
    )
    {
        var registryEntry = Registry.RegisterOperation(operationDefinition);

        var operationBuilder = new OperationBuilder<TId, TArgs, TResult>(registryEntry, Services);

        configure(operationBuilder);
    }
};
