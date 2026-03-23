namespace DuOps.Npgsql.Dtos;

internal sealed record InnerResultDto(
    string InnerResulttype,
    string? InnerResultId,
    string Value,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
