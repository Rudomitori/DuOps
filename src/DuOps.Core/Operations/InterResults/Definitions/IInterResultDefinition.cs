namespace DuOps.Core.Operations.InterResults.Definitions;

public interface IInterResultDefinition<TValue> : IInterResultDefinition
{
    string SerializeValue(TValue result);

    TValue DeserializeValue(string serializedResult);
}

public interface IInterResultDefinition<TKey, TValue> : IInterResultDefinition
{
    string SerializeValue(TValue result);

    TValue DeserializeValue(string serializedResult);

    string SerializeKey(TKey key);

    TKey DeserializeKey(string serializedKey);
}

public interface IInterResultDefinition
{
    public InterResultDiscriminator Discriminator { get; }
}
