namespace DuOps.Npgsql.Dtos;

internal sealed record OperationDto(
    string Type,
    string Id,
    string QueueId,
    DateTime? ScheduledAt,
    string Args,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    short State,
    string? Result,
    string? FailReason,
    int RetryCount
);
