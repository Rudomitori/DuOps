namespace DuOps.Core.OperationDefinitions;

public readonly record struct OperationDiscriminator(string Value)
{
    public override string ToString() => Value;
}
