namespace DuOps.Core.Operations.InterResults.Definitions;

// TODO: Restrict possible values to [a-zA-Z0-9_]?
public readonly record struct InterResultDiscriminator(string Value)
{
    public override string ToString() => Value;

    public static implicit operator string(InterResultDiscriminator discriminator) =>
        discriminator.Value;
}
