using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Registry;

public interface IOperationDefinitionRegistry
{
    Task<TCallbackResult> InvokeCallbackWithDefinition<TCallback, TCallbackResult>(
        OperationDiscriminator discriminator,
        TCallback callback
    )
        where TCallback : IOperationDefinitionGenericCallback<TCallbackResult>;
}

public interface IOperationDefinitionGenericCallback<TCallbackResult>
{
    Task<TCallbackResult> Invoke<TOperationArgs, TOperationResult>(
        IOperationDefinition<TOperationArgs, TOperationResult> operationDefinition
    );
}
