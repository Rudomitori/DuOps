namespace DuOps.Core.OperationDefinitions.RetryPolicies;

public sealed class AdHocOperationRetryingPolicy : IOperationRetryPolicy
{
    private readonly Func<Exception, int, bool> _shouldRetry;
    private readonly Func<Exception, int, TimeSpan> _retryDelay;

    public AdHocOperationRetryingPolicy(
        Func<Exception, int, bool> shouldRetry,
        Func<Exception, int, TimeSpan> retryDelay
    )
    {
        ArgumentNullException.ThrowIfNull(shouldRetry);
        ArgumentNullException.ThrowIfNull(retryDelay);

        _shouldRetry = shouldRetry;
        _retryDelay = retryDelay;
    }

    public bool ShouldRetry(Exception exception, int retryCount) =>
        _shouldRetry(exception, retryCount);

    public TimeSpan RetryDelay(Exception exception, int retryCount) =>
        _retryDelay(exception, retryCount);
}
