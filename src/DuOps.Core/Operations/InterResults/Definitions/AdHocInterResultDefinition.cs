namespace DuOps.Core.Operations.InterResults.Definitions;

public sealed class AdHocInterResultDefinition<TResult> : IInterResultDefinition<TResult>
{
    private readonly Func<TResult, string> _serialize;
    private readonly Func<string, TResult> _deserialize;

    public InterResultDiscriminator Discriminator { get; }

    public AdHocInterResultDefinition(
        InterResultDiscriminator discriminator,
        Func<TResult, string> serialize,
        Func<string, TResult> deserialize
    )
    {
        Discriminator = discriminator;
        _deserialize = deserialize;
        _serialize = serialize;
    }

    public string SerializeResult(TResult result) => _serialize(result);

    public TResult DeserializeResult(string serializedResult) => _deserialize(serializedResult);
}
