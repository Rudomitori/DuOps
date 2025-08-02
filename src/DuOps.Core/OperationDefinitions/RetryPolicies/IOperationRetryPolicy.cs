namespace DuOps.Core.OperationDefinitions.RetryPolicies;

public interface IOperationRetryPolicy
{
    bool ShouldRetry(Exception exception, int retryCount);

    TimeSpan RetryDelay(Exception exception, int retryCount);
}
