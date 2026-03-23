using DuOps.Core.Serializers;

namespace DuOps.Core.InnerResults;

public sealed class InnerResultDefinition<TResult> : IInnerResultDefinition<TResult>
{
    public required InnerResultType Type { get; init; }
    public required ISerializer<TResult> ValueSerializer { get; init; }
}

public sealed class InnerResultDefinition<TId, TValue> : IInnerResultDefinition<TId, TValue>
{
    public required InnerResultType Type { get; init; }
    public required ISerializer<TId> IdSerializer { get; init; }
    public required ISerializer<TValue> ValueSerializer { get; init; }
}

public static class InnerResultDefinition
{
    public static InnerResultDefinition<TResult> Create<TResult>(
        InnerResultType type,
        ISerializer<TResult> valueSerializer
    )
    {
        return new InnerResultDefinition<TResult>
        {
            Type = type,
            ValueSerializer = valueSerializer,
        };
    }

    public static InnerResultDefinition<TId, TResult> Create<TId, TResult>(
        InnerResultType type,
        ISerializer<TId> idSerializer,
        ISerializer<TResult> valueSerializer
    )
    {
        return new InnerResultDefinition<TId, TResult>
        {
            Type = type,
            IdSerializer = idSerializer,
            ValueSerializer = valueSerializer,
        };
    }
}
