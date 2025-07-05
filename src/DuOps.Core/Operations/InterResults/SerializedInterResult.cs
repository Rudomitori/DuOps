namespace DuOps.Core.Operations.InterResults;

public readonly record struct SerializedInterResult(string Value)
{
    public override string ToString() => Value;
}
