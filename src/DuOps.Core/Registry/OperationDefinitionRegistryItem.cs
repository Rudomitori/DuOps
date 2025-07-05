using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Registry;

internal sealed record OperationDefinitionRegistryItem<TArgs, TResult>(
    IOperationDefinition<TArgs, TResult> OperationDefinition
) : IOperationDefinitionRegistryItem
{
    IOperationDefinition IOperationDefinitionRegistryItem.OperationDefinition =>
        OperationDefinition;

    public Task CallGenericCallback(IOperationDefinitionGenericCallback callback)
    {
        return callback.Invoke(OperationDefinition);
    }
}

internal interface IOperationDefinitionRegistryItem
{
    IOperationDefinition OperationDefinition { get; }

    Task CallGenericCallback(IOperationDefinitionGenericCallback callback);
}
