namespace DuOps.Npgsql.Dtos;

public sealed record GetNextForExecutionDto(string Type, string Id, string Args, int RetryCount);
