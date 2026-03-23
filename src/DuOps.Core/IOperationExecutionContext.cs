using DuOps.Core.InnerResults;
using DuOps.Core.Operations;

namespace DuOps.Core;

public interface IOperationExecutionContext
{
    Task AddInnerResult<TValue>(
        IInnerResultDefinition<TValue> resultDefinition,
        TValue value,
        CancellationToken cancellationToken = default
    );

    InnerResult<TValue>? GetInnerResultOrNull<TValue>(IInnerResultDefinition<TValue> definition);

    InnerResult<TId, TValue>? GetInnerResultOrNull<TId, TValue>(
        IInnerResultDefinition<TId, TValue> definition,
        TId id
    );

    InnerResult<TId, TValue>[] GetInnerResults<TId, TValue>(
        IInnerResultDefinition<TId, TValue> definition
    );

    Task<TValue> RunWithCache<TValue>(
        IInnerResultDefinition<TValue> innerResultDefinition,
        Func<Task<TValue>> action
    );

    Task<TValue> RunWithCache<TId, TValue>(
        IInnerResultDefinition<TId, TValue> definition,
        TId id,
        Func<Task<TValue>> action
    );

    public Task Wait(string reason, TimeSpan duration);

    public Task Wait(string reason, DateTime until);

    SerializedOperationId SerializedOperationId { get; }

    CancellationToken YieldToken { get; }
}
