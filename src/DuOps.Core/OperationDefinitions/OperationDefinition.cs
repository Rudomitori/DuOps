using DuOps.Core.Serializers;

namespace DuOps.Core.OperationDefinitions;

public sealed record OperationDefinition<TId, TArgs, TResult>(
    OperationType Type,
    ISerializer<TId> IdSerializer,
    ISerializer<TArgs> ArgsSerializer,
    ISerializer<TResult> ResultSerializer
) : IOperationDefinition<TId, TArgs, TResult>;

public static class AdHocOperationDefinition
{
    public static OperationDefinition<TId, TArgs, TResult> Create<TId, TArgs, TResult>(
        string type,
        ISerializer<TId> idSerializer,
        ISerializer<TArgs> argsSerializer,
        ISerializer<TResult> resultSerializer
    )
    {
        return new OperationDefinition<TId, TArgs, TResult>(
            new OperationType(type),
            idSerializer,
            argsSerializer,
            resultSerializer
        );
    }
}
