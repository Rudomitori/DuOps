namespace DuOps.Npgsql;

public sealed class NpgsqlOperationStorageOptions
{
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan LockExtendingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan GetNextInterval { get; set; } = TimeSpan.FromSeconds(0.2);
}
