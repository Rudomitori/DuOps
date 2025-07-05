namespace DuOps.Core.Operations.InterResults.Definitions;

public readonly record struct InterResultDiscriminator(string Value)
{
    public override string ToString() => Value;
}
