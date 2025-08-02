namespace DuOps.Core.Operations;

public abstract record SerializedOperationState
{
    public sealed record Created : SerializedOperationState
    {
        public static readonly Created Instance = new();
    }

    public abstract record Yielded : SerializedOperationState;

    public sealed record Waiting(DateTimeOffset Until) : SerializedOperationState;

    public sealed record Retrying(DateTimeOffset At, int RetryCount) : SerializedOperationState;

    public sealed record Finished(SerializedOperationResult Result) : SerializedOperationState;

    public sealed record Failed(string Reason) : SerializedOperationState;
}
