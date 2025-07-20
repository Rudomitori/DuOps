namespace DuOps.Core.Operations.InterResults;

public readonly record struct SerializedInterResultValue(string Value)
{
    public static implicit operator string(SerializedInterResultValue value) => value.Value;

    public override string ToString() => Value;
}
