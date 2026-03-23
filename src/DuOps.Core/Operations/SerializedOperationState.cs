namespace DuOps.Core.Operations;

public abstract record SerializedOperationState
{
    public sealed record Active : SerializedOperationState
    {
        public static readonly Active Instance = new();
    }

    public sealed record Completed(DateTime At, SerializedOperationResult Result)
        : SerializedOperationState;

    public sealed record Failed(DateTime At, string Reason) : SerializedOperationState;
}
