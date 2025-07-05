namespace DuOps.Core.Operations.InterResults.Definitions;

public sealed class AdHocKeyedInterResultDefinition<TResult, TKey>: IKeyedInterResultDefinition<TResult, TKey>
{
    private readonly Func<TResult, string> _serialize;
    private readonly Func<string, TResult> _deserialize;
    private readonly Func<TKey, string> _serializeKey;
    private readonly Func<string, TKey> _deserializeKey;

    public InterResultDiscriminator Discriminator { get; }

    public AdHocKeyedInterResultDefinition(
        InterResultDiscriminator discriminator,
        Func<TResult, string> serialize,
        Func<string, TResult> deserialize,
        Func<TKey, string> serializeKey,
        Func<string, TKey> deserializeKey
    )
    {
        Discriminator = discriminator;
        _deserialize = deserialize;
        _serializeKey = serializeKey;
        _deserializeKey = deserializeKey;
        _serialize = serialize;
    }

    public string Serialize(
        TResult result
    ) => _serialize(result);

    public TKey DeserializeKey(
        string serializedKey
    ) => _deserializeKey(serializedKey);

    public string SerializeKey(
        TKey key
    ) => _serializeKey(key);

    public TResult Deserialize(
        string serializedResult
    ) => _deserialize(serializedResult);
}
