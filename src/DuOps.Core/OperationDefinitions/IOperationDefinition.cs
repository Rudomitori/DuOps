using DuOps.Core.Serializers;

namespace DuOps.Core.OperationDefinitions;

public interface IOperationDefinition<TId, TArgs, TResult> : IOperationDefinition
{
    ISerializer<TId> IdSerializer { get; }
    ISerializer<TArgs> ArgsSerializer { get; }
    ISerializer<TResult> ResultSerializer { get; }
}

public interface IOperationDefinition
{
    OperationType Type { get; }
}
