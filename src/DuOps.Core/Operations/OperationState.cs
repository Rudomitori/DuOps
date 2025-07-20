namespace DuOps.Core.Operations;

public abstract record OperationState<TResult>
{
    public sealed record Yielded : OperationState<TResult>
    {
        public static readonly Yielded Instance = new();
    }

    public sealed record Finished(TResult Result) : OperationState<TResult>;
}

public static class OperationState
{
    public static OperationState<T>.Finished FromResult<T>(T result) => new(result);
}
