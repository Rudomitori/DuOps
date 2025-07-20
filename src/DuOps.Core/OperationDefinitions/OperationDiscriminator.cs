namespace DuOps.Core.OperationDefinitions;

// TODO: Restrict possible values to [a-zA-Z0-9_]?
public readonly record struct OperationDiscriminator(string Value)
{
    public override string ToString() => Value;
}
