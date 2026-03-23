using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationDefinitions.RetryPolicies;

namespace DuOps.Core.Registry;

internal sealed class OperationRegistry
{
    private Dictionary<OperationType, OperationRegistryEntry> _entries = new();

    internal OperationRegistryEntry<TId, TArgs, TResult> RegisterOperation<TId, TArgs, TResult>(
        IOperationDefinition<TId, TArgs, TResult> operationDefinition
    )
    {
        if (_entries.ContainsKey(operationDefinition.Type))
        {
            throw new InvalidOperationException(
                $"Operation {operationDefinition.Type} is already registered"
            );
        }

        var entry = new OperationRegistryEntry<TId, TArgs, TResult>(operationDefinition);
        _entries.Add(operationDefinition.Type, entry);

        return entry;
    }

    public TCallbackResult InvokeCallbackWithEntry<TCallback, TCallbackResult>(
        OperationType type,
        TCallback callback
    )
        where TCallback : IOperationRegistryCallback<TCallbackResult>
    {
        var entry = _entries.GetValueOrDefault(type);
        if (entry is null)
            throw new InvalidOperationException($"Operation {type} is not registered");

        return entry.CallGenericCallback(callback);
    }

    public IOperationRetryPolicy? GetRetryPolicyOrDefault(OperationType type)
    {
        var registryItem = _entries.GetValueOrDefault(type);
        return registryItem?.RetryPolicy;
    }
}
