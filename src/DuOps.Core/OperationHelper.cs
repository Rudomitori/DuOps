using DuOps.Core.Exceptions;

namespace DuOps.Core;

internal static class OperationHelper
{
    internal static string SerializeInterResultKey<TKey>(
        Func<TKey, string> serialize,
        TKey key
    )
    {
        try
        {
            return serialize(key);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to serialize key of inter result",
                e
            );
        }
    }

    internal static TKey DeserializeInterResultKey<TKey>(
        Func<string, TKey> deserialize,
        string serializedKey
    )
    {
        try
        {
            return deserialize(serializedKey);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to deserialize key of inter result",
                e
            );
        }
    }

    internal static string SerializeInterResultValue<TResult>(
        Func<TResult, string> serialize,
        TResult resultValue
    )
    {
        try
        {
            return serialize(resultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inter result",
                e
            );
        }
    }

    internal static TResult DeserializeInterResultValue<TResult>(
        Func<string, TResult> deserialize,
        string serializedResultValue
    )
    {
        try
        {
            return deserialize(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize value of inter result",
                e
            );
        }
    }

    internal static string SerializeArgs<TArgs>(
        TArgs args,
        Func<TArgs, string> serialize
    )
    {
        try
        {
            return serialize(args);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to serialize args of operation",
                e
            );
        }
    }

    internal static TArgs DeserializeArgs<TArgs>(
        string serializedArgs,
        Func<string, TArgs> deserialize
    )
    {
        try
        {
            return deserialize(serializedArgs);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to deserialize args of operation",
                e
            );
        }
    }

    internal static string SerializeResult<TResult>(
        TResult value,
        Func<TResult, string> serialize
    )
    {
        try
        {
            return serialize(value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to serialize value of operation result",
                e
            );
        }
    }

    internal static TResult DeserializeResult<TResult>(
        string serializedValue,
        Func<string, TResult> deserialize
    )
    {
        try
        {
            return deserialize(serializedValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                "Failed to deserialize value of operation result",
                e
            );
        }
    }
}
