using DuOps.Core.OperationDefinitions.RetryPolicies;
using DuOps.Core.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.DependencyInjection;

public sealed class OperationBuilder<TId, TArgs, TResult>
{
    private readonly OperationRegistryEntry<TId, TArgs, TResult> _registryEntry;
    private readonly IServiceCollection _services;

    internal OperationBuilder(
        OperationRegistryEntry<TId, TArgs, TResult> registryEntry,
        IServiceCollection services
    )
    {
        _registryEntry = registryEntry;
        _services = services;
    }

    #region AddHandler

    public void AddScopedHandler<THandler>()
        where THandler : class, IOperationHandler<TId, TArgs, TResult>
    {
        _services.AddScoped<THandler>();

        AddHandler(serviceProvider => serviceProvider.GetRequiredService<THandler>());
    }

    public void AddHandler(Func<IServiceProvider, IOperationHandler<TId, TArgs, TResult>> factory)
    {
        _registryEntry.OperationHandlerFactory = factory;
    }

    #endregion

    public IOperationRetryPolicy RetryPolicy
    {
        set => _registryEntry.RetryPolicy = value;
    }
}
