namespace DuOps.Npgsql.Dtos;

public enum OperationStateDto
{
    Created = 10,
    Waiting = 20,
    Retrying = 30,
    Finished = 40,
    Failed = 50,
}
