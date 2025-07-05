using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationPollers;

public interface IOperationExecutionContext
{
    Task AddInterResult<TResult>(
        IInterResultDefinition<TResult> resultDefinition,
        TResult value
    );

    InterResult<TResult>? GetInterResultOrDefault<TResult>(
        IInterResultDefinition<TResult> resultDefinition
    );

    KeyedInterResult<TResult, TKey>? GetInterResultOrDefault<TResult, TKey>(
        IKeyedInterResultDefinition<TResult, TKey> resultDefinition,
        TKey key
    );

    IReadOnlyCollection<KeyedInterResult<TResult, TKey>> GetInterResultsOrDefault<TResult, TKey>(
        IKeyedInterResultDefinition<TResult, TKey> resultDefinition
    );

    Task<TResult> RunWithCache<TResult>(
        IInterResultDefinition<TResult> resultDefinition,
        Func<Task<TResult>> action
    );

    Task<TResult> RunWithCache<TResult, TKey>(
        IKeyedInterResultDefinition<TResult, TKey> resultDefinition,
        TKey key,
        Func<Task<TResult>> action
    );

    OperationId OperationId { get; }

    CancellationToken YieldToken { get; }
}
