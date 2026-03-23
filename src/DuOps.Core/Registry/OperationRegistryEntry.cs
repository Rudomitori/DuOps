using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationDefinitions.RetryPolicies;

namespace DuOps.Core.Registry;

internal sealed class OperationRegistryEntry<TId, TArgs, TResult> : OperationRegistryEntry
{
    public IOperationDefinition<TId, TArgs, TResult> OperationDefinition { get; private init; }
    public Func<
        IServiceProvider,
        IOperationHandler<TId, TArgs, TResult>
    >? OperationHandlerFactory { get; set; }

    public OperationRegistryEntry(IOperationDefinition<TId, TArgs, TResult> operationDefinition)
    {
        OperationDefinition = operationDefinition;
    }

    internal override TCallbackResult CallGenericCallback<TCallbackResult>(
        IOperationRegistryCallback<TCallbackResult> callback
    )
    {
        return callback.Invoke(this);
    }
}

internal abstract class OperationRegistryEntry
{
    internal IOperationRetryPolicy? RetryPolicy { get; set; }

    internal abstract TCallbackResult CallGenericCallback<TCallbackResult>(
        IOperationRegistryCallback<TCallbackResult> callback
    );
}
