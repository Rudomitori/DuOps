namespace DuOps.Core.Operations;

public readonly record struct SerializedOperationId(string Value)
{
    public override string ToString() => Value;
}
