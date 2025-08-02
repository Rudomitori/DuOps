using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationPollers;

public interface IOperationExecutionContext
{
    Task AddInterResult<TValue>(
        IInterResultDefinition<TValue> resultDefinition,
        TValue value,
        CancellationToken cancellationToken = default
    );

    InterResult<TValue>? GetInterResultOrNull<TValue>(IInterResultDefinition<TValue> definition);

    InterResult<TKey, TValue>? GetInterResultOrNull<TKey, TValue>(
        IInterResultDefinition<TKey, TValue> definition,
        TKey key
    );

    IReadOnlyCollection<InterResult<TKey, TValue>> GetInterResults<TKey, TValue>(
        IInterResultDefinition<TKey, TValue> definition
    );

    Task<TValue> RunWithCache<TValue>(
        IInterResultDefinition<TValue> definition,
        Func<Task<TValue>> action
    );

    Task<TValue> RunWithCache<TKey, TValue>(
        IInterResultDefinition<TKey, TValue> definition,
        TKey key,
        Func<Task<TValue>> action
    );

    public Task Wait(string reason, TimeSpan duration);

    public Task Wait(string reason, DateTimeOffset until);

    OperationId OperationId { get; }

    CancellationToken YieldToken { get; }
}
