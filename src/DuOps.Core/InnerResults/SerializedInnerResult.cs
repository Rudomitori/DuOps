namespace DuOps.Core.InnerResults;

public sealed record SerializedInnerResult(
    InnerResultType Type,
    SerializedInnerResultId? Id,
    SerializedInnerResultValue Value,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
