namespace DuOps.Core.Operations.InterResults.Definitions;

public interface IInterResultDefinition<TResult>: IInterResultDefinition
{
    string Serialize(
        TResult result
    );

    TResult Deserialize(
        string serializedResult
    );
}

public interface IKeyedInterResultDefinition<TResult, TKey>: IInterResultDefinition
{
    string Serialize(
        TResult result
    );

    TResult Deserialize(
        string serializedResult
    );

    string SerializeKey(
        TKey key
    );

    TKey DeserializeKey(
        string serializedKey
    );
}

public interface IInterResultDefinition
{
    public InterResultDiscriminator Discriminator { get; }
}
