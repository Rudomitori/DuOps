namespace DuOps.Core.Operations;

public abstract record OperationExecutionResult<TSagaResult>
{
    public sealed record Yielded : OperationExecutionResult<TSagaResult>
    {
        public static readonly Yielded Instance = new();
    }

    public sealed record Finished(
        TSagaResult Result
    ) : OperationExecutionResult<TSagaResult>;
}
