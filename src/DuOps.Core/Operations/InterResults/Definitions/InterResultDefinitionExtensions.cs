using DuOps.Core.Exceptions;

namespace DuOps.Core.Operations.InterResults.Definitions;

public static class InterResultDefinitionExtensions
{
    #region Key

    internal static SerializedInterResultKey Serialize<TValue, TKey>(
        this IKeyedInterResultDefinition<TValue, TKey> definition,
        InterResultKey<TKey> key
    )
    {
        try
        {
            var serializeKeyValue = definition.SerializeKey(key.Value);
            return new SerializedInterResultKey(serializeKeyValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize key of inter result {definition.Discriminator}",
                e
            );
        }
    }

    internal static InterResultKey<TKey> Deserialize<TValue, TKey>(
        this IKeyedInterResultDefinition<TValue, TKey> definition,
        SerializedInterResultKey serializedKey
    )
    {
        try
        {
            var serializeKeyValue = definition.DeserializeKey(serializedKey.Value);
            return new InterResultKey<TKey>(serializeKeyValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize key of inter result {definition.Discriminator}",
                e
            );
        }
    }

    #endregion

    #region Result

    internal static SerializedInterResult Serialize<TResult>(
        this IInterResultDefinition<TResult> definition,
        InterResult<TResult> result
    )
    {
        try
        {
            var serializedResultValue = definition.SerializeResult(result.Value);
            return new SerializedInterResult(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inter result {definition.Discriminator}",
                e
            );
        }
    }

    internal static InterResult<TResult> Deserialize<TResult>(
        this IInterResultDefinition<TResult> definition,
        SerializedInterResult serializedResult
    )
    {
        try
        {
            var resultValue = definition.DeserializeResult(serializedResult.Value);
            return new InterResult<TResult>(resultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize value of inter result {definition.Discriminator}",
                e
            );
        }
    }

    #endregion
}
