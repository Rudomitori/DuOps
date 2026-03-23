namespace DuOps.Core.InnerResults;

public sealed record InnerResult<TValue>(
    InnerResultType Type,
    TValue Value,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record InnerResult<TId, TValue>(
    InnerResultType Type,
    TId Id,
    TValue Value,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
