namespace DuOps.Core.Operations.InterResults.Definitions;

public sealed record NullInterResultDefinition(InterResultDiscriminator Discriminator)
    : IInterResultDefinition<object?>
{
    public object? DeserializeResult(string serializedResult) => null;

    public string SerializeResult(object? result) => "";

    public static NullInterResultDefinition From(string discriminator)
    {
        return new NullInterResultDefinition(new InterResultDiscriminator(discriminator));
    }
}
