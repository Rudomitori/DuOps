namespace DuOps.Core.Operations;

public abstract record OperationState<TResult>
{
    public sealed record Active : OperationState<TResult>
    {
        public static readonly Active Instance = new();
    }

    public sealed record Competed(DateTime At, TResult Result) : OperationState<TResult>;

    public sealed record Failed(DateTime At, string Reason) : OperationState<TResult>;
}

public static class OperationState
{
    public static OperationState<T>.Competed FromResult<T>(DateTime at, T result)
    {
        return new OperationState<T>.Competed(at, result);
    }
}
