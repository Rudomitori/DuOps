using DuOps.Core.OperationDefinitions;

namespace DuOps.Core.Registry;

public interface IOperationDefinitionRegistry
{
    Task InvokeCallbackWithDefinition<TCallback>(
        OperationDiscriminator discriminator,
        TCallback callback
    ) where TCallback: IOperationDefinitionGenericCallback;
}

public interface IOperationDefinitionGenericCallback
{
    Task Invoke<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition
    );
}
