namespace DuOps.Core.Operations;

public readonly record struct SerializedOperationArgs(string Value)
{
    public override string ToString() => Value;
}
