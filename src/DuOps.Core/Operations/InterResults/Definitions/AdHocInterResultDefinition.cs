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

    public string SerializeValue(TResult result) => _serialize(result);

    public TResult DeserializeValue(string serializedResult) => _deserialize(serializedResult);
}

public sealed class AdHocInterResultDefinition<TKey, TValue> : IInterResultDefinition<TKey, TValue>
{
    private readonly Func<TValue, string> _serialize;
    private readonly Func<string, TValue> _deserialize;
    private readonly Func<TKey, string> _serializeKey;
    private readonly Func<string, TKey> _deserializeKey;

    public InterResultDiscriminator Discriminator { get; }

    public AdHocInterResultDefinition(
        InterResultDiscriminator discriminator,
        Func<TValue, string> serialize,
        Func<string, TValue> deserialize,
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

    public string SerializeValue(TValue result) => _serialize(result);

    public TKey DeserializeKey(string serializedKey) => _deserializeKey(serializedKey);

    public string SerializeKey(TKey key) => _serializeKey(key);

    public TValue DeserializeValue(string serializedResult) => _deserialize(serializedResult);
}
