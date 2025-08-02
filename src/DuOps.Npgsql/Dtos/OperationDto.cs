namespace DuOps.Npgsql.Dtos;

internal sealed record OperationDto(
    string Discriminator,
    string Id,
    string? PollingScheduleId,
    DateTime StartedAt,
    string Args,
    OperationStateDto State,
    string? Result,
    DateTime? WaitingUntil,
    DateTime? RetryingAt,
    int? RetryCount,
    string? FailReason,
    string InterResults
);
