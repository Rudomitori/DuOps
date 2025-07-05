namespace DuOps.Core.Operations.InterResults.Definitions;

public interface IInterResultDefinition<TResult> : IInterResultDefinition
{
    string SerializeResult(TResult result);

    TResult DeserializeResult(string serializedResult);
}

public interface IKeyedInterResultDefinition<TResult, TKey> : IInterResultDefinition<TResult>
{
    string SerializeKey(TKey key);

    TKey DeserializeKey(string serializedKey);
}

public interface IInterResultDefinition
{
    public InterResultDiscriminator Discriminator { get; }
}
