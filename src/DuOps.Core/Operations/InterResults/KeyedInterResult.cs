namespace DuOps.Core.Operations.InterResults;

public sealed record KeyedInterResult<TValue, TKey>(TKey Key, TValue Value);
