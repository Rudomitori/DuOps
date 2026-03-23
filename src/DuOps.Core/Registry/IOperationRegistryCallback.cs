namespace DuOps.Core.Registry;

internal interface IOperationRegistryCallback<TCallbackResult>
{
    TCallbackResult Invoke<TId, TArgs, TResult>(
        OperationRegistryEntry<TId, TArgs, TResult> registryEntry
    );
}
