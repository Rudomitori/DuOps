namespace DuOps.Core.Operations.InterResults.Definitions;

public sealed record NullInterResultDefinition(InterResultDiscriminator Discriminator): IInterResultDefinition<object?>
{
    public object? Deserialize(
        string serializedResult
    ) => null;

    public string Serialize(
        object? result
    ) => "";

    public static NullInterResultDefinition From(
        string discriminator
    )
    {
        return new NullInterResultDefinition(new InterResultDiscriminator(discriminator));
    }
}
