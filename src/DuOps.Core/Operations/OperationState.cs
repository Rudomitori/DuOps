namespace DuOps.Core.Operations;

public abstract record OperationState<TResult>
{
    public sealed record Created : OperationState<TResult>
    {
        public static readonly Created Instance = new();
    }

    public record Yielded : OperationState<TResult>
    {
        public static readonly Yielded Instance = new();
    }

    public sealed record Waiting(DateTimeOffset Until) : OperationState<TResult>;

    public sealed record Retrying(DateTimeOffset At, int RetryCount) : OperationState<TResult>;

    public sealed record Finished(TResult Result) : OperationState<TResult>;

    public sealed record Failed(string Reason) : OperationState<TResult>;
}

public static class OperationState
{
    public static OperationState<T>.Finished FromResult<T>(T result) => new(result);
}
