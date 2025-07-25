namespace DuOps.Core.Operations;

public readonly record struct SerializedOperationResult(string Value)
{
    public override string ToString() => Value;
}
