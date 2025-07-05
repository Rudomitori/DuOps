namespace DuOps.Core.Operations;

public abstract record SerializedOperationState
{
    public sealed record Yielded : SerializedOperationState
    {
        public static readonly Yielded Instance = new();
    }

    public sealed record Finished(SerializedOperationResult Result) : SerializedOperationState;
}
