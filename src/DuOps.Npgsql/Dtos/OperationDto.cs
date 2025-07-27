namespace DuOps.Npgsql.Dtos;

internal sealed record OperationDto(
    string Discriminator,
    string Id,
    string? PollingScheduleId,
    DateTime StartedAt,
    string Args,
    bool IsFinished,
    string? Result,
    string InterResults
);
