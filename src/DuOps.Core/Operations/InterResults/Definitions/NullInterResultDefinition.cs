namespace DuOps.Core.Operations.InterResults.Definitions;

public sealed record NullInterResultDefinition(InterResultDiscriminator Discriminator)
    : IInterResultDefinition<object?>
{
    public object? DeserializeValue(string serializedResult) => null;

    public string SerializeValue(object? result) => "";

    public static NullInterResultDefinition From(string discriminator)
    {
        return new NullInterResultDefinition(new InterResultDiscriminator(discriminator));
    }
}
