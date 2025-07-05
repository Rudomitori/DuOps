namespace DuOps.Core.Operations;

public abstract record OperationState<T>
{
    public sealed record Yielded : OperationState<T>
    {
        public static readonly Yielded Instance = new();
    }

    public sealed record Finished(OperationResult<T> Result) : OperationState<T>;
}
