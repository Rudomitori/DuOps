using DuOps.Core.OperationDefinitions.RetryPolicies;

namespace DuOps.Core.OperationDefinitions;

public interface IOperationDefinition<TArgs, TResult> : IOperationDefinition
{
    string SerializeArgs(TArgs args);

    TArgs DeserializeArgs(string serializedArgs);

    string SerializeResult(TResult result);

    TResult DeserializeResult(string serializedResult);
}

public interface IOperationDefinition
{
    OperationDiscriminator Discriminator { get; }

    IOperationRetryPolicy RetryPolicy { get; }
}
