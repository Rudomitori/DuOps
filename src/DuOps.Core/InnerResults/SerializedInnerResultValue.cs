namespace DuOps.Core.InnerResults;

public readonly record struct SerializedInnerResultValue(string Value)
{
    public static implicit operator string(SerializedInnerResultValue value) => value.Value;

    public override string ToString() => Value;
}
