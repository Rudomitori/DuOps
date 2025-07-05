using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationPollers;

public interface IOperationExecutionContext
{
    Task AddInterResult<TResult>(IInterResultDefinition<TResult> resultDefinition, TResult value);

    InterResult<TResult>? GetInterResultOrNull<TResult>(IInterResultDefinition<TResult> definition);

    InterResult<TResult>? GetInterResultOrNull<TResult, TKey>(
        IKeyedInterResultDefinition<TResult, TKey> definition,
        TKey key
    );

    IReadOnlyCollection<KeyValuePair<InterResultKey<TKey>, InterResult<TResult>>> GetInterResults<
        TResult,
        TKey
    >(IKeyedInterResultDefinition<TResult, TKey> resultDefinition);

    Task<TResult> RunWithCache<TResult>(
        IInterResultDefinition<TResult> definition,
        Func<Task<TResult>> action
    );

    Task<TResult> RunWithCache<TResult, TKey>(
        IKeyedInterResultDefinition<TResult, TKey> definition,
        TKey keyValue,
        Func<Task<TResult>> action
    );

    OperationId OperationId { get; }

    CancellationToken YieldToken { get; }
}
