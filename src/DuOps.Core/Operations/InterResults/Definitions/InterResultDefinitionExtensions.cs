using System.Diagnostics;
using DuOps.Core.Exceptions;

namespace DuOps.Core.Operations.InterResults.Definitions;

public static class InterResultDefinitionExtensions
{
    #region NewInterResult

    public static InterResult<TValue> NewInterResult<TValue>(
        this IInterResultDefinition<TValue> definition,
        TValue value
    )
    {
        return new InterResult<TValue>(definition.Discriminator, value);
    }

    public static InterResult<TKey, TValue> NewInterResult<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        TKey key,
        TValue value
    )
    {
        return new InterResult<TKey, TValue>(definition.Discriminator, key, value);
    }

    #endregion

    #region InterResult serializetion

    public static SerializedInterResult Serialize<TValue>(
        this IInterResultDefinition<TValue> definition,
        InterResult<TValue> interResult
    )
    {
        Debug.Assert(definition.Discriminator == interResult.Discriminator);

        return new SerializedInterResult(
            definition.Discriminator,
            Key: null,
            definition.SerializeValueAndWrapException(interResult.Value)
        );
    }

    public static SerializedInterResult Serialize<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        InterResult<TKey, TValue> interResult
    )
    {
        Debug.Assert(definition.Discriminator == interResult.Discriminator);

        return new SerializedInterResult(
            definition.Discriminator,
            definition.SerializeKeyAndWrapException(interResult.Key),
            definition.SerializeValueAndWrapException(interResult.Value)
        );
    }

    #endregion

    #region Key serialization

    public static SerializedInterResultKey SerializeKeyAndWrapException<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        TKey key
    )
    {
        try
        {
            var serializeKeyValue = definition.SerializeKey(key);
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

    public static TKey DeserializeKeyAndWrapException<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        SerializedInterResultKey serializedKey
    )
    {
        try
        {
            return definition.DeserializeKey(serializedKey.Value);
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

    #region Value serialization

    public static SerializedInterResultValue SerializeValueAndWrapException<TValue>(
        this IInterResultDefinition<TValue> definition,
        TValue value
    )
    {
        try
        {
            var serializedResultValue = definition.SerializeValue(value);
            return new SerializedInterResultValue(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inter result {definition.Discriminator}",
                e
            );
        }
    }

    public static TValue DeserializeValueAndWrapException<TValue>(
        this IInterResultDefinition<TValue> definition,
        SerializedInterResultValue serializedValue
    )
    {
        try
        {
            return definition.DeserializeValue(serializedValue.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize value of inter result {definition.Discriminator}",
                e
            );
        }
    }

    public static SerializedInterResultValue SerializeValueAndWrapException<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        TValue value
    )
    {
        try
        {
            var serializedResultValue = definition.SerializeValue(value);
            return new SerializedInterResultValue(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inter result {definition.Discriminator}",
                e
            );
        }
    }

    public static TValue DeserializeValueAndWrapException<TKey, TValue>(
        this IInterResultDefinition<TKey, TValue> definition,
        SerializedInterResultValue serializedValue
    )
    {
        try
        {
            return definition.DeserializeValue(serializedValue.Value);
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
