﻿using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Registry;

internal sealed class OperationDefinitionRegistry(
    IEnumerable<IOperationDefinitionRegistryItem> registryItems
) : IOperationDefinitionRegistry
{
    public Task<TCallbackResult> InvokeCallbackWithDefinition<TCallback, TCallbackResult>(
        OperationDiscriminator discriminator,
        TCallback callback
    )
        where TCallback : IOperationDefinitionGenericCallback<TCallbackResult>
    {
        var registryItem = registryItems.FirstOrDefault(x =>
            x.OperationDefinition.Discriminator == discriminator
        );
        if (registryItem is null)
        {
            throw new InvalidOperationException(
                $"Operation definition {discriminator.Value} is not properly registered in DI"
            );
        }

        return registryItem.CallGenericCallback(callback);
    }
}
