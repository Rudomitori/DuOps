using DuOps.Core.Serializers;

namespace DuOps.Core.InnerResults;

public interface IInnerResultDefinition<TValue> : IInnerResultDefinition
{
    ISerializer<TValue> ValueSerializer { get; }
}

public interface IInnerResultDefinition<TId, TValue> : IInnerResultDefinition
{
    ISerializer<TId> IdSerializer { get; }
    ISerializer<TValue> ValueSerializer { get; }
}

public interface IInnerResultDefinition
{
    public InnerResultType Type { get; }
}
