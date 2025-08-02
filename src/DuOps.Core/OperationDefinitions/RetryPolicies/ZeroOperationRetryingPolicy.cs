namespace DuOps.Core.OperationDefinitions.RetryPolicies;

public sealed class ZeroOperationRetryingPolicy : IOperationRetryPolicy
{
    public static readonly ZeroOperationRetryingPolicy Instance = new();

    public bool ShouldRetry(Exception exception, int retryCount) => false;

    public TimeSpan RetryDelay(Exception exception, int retryCount) => TimeSpan.Zero;
}
