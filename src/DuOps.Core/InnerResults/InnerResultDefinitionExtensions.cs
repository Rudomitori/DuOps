using System.Diagnostics;
using DuOps.Core.Exceptions;

namespace DuOps.Core.InnerResults;

public static class InnerResultDefinitionExtensions
{
    #region NewInnerResult

    public static InnerResult<TValue> NewInnerResult<TValue>(
        this IInnerResultDefinition<TValue> definition,
        TValue value,
        DateTime now
    )
    {
        return new InnerResult<TValue>(definition.Type, value, CreatedAt: now, UpdatedAt: null);
    }

    #endregion

    #region InnerResult serializetion

    public static SerializedInnerResult Serialize<TValue>(
        this IInnerResultDefinition<TValue> definition,
        InnerResult<TValue> innerResult
    )
    {
        Debug.Assert(definition.Type == innerResult.Type);

        return new SerializedInnerResult(
            definition.Type,
            Id: null,
            definition.SerializeValue(innerResult.Value),
            CreatedAt: innerResult.CreatedAt,
            UpdatedAt: innerResult.UpdatedAt
        );
    }

    public static SerializedInnerResult Serialize<TId, TValue>(
        this IInnerResultDefinition<TId, TValue> definition,
        InnerResult<TId, TValue> innerResult
    )
    {
        Debug.Assert(definition.Type == innerResult.Type);

        return new SerializedInnerResult(
            definition.Type,
            definition.SerializeId(innerResult.Id),
            definition.SerializeValue(innerResult.Value),
            CreatedAt: innerResult.CreatedAt,
            UpdatedAt: innerResult.UpdatedAt
        );
    }

    #endregion

    #region InnerResult deserialization

    public static InnerResult<TValue> Deserialize<TValue>(
        this IInnerResultDefinition<TValue> definition,
        SerializedInnerResult serializedInnerResult
    )
    {
        Debug.Assert(definition.Type == serializedInnerResult.Type);

        return new InnerResult<TValue>(
            definition.Type,
            definition.DeserializeValue(serializedInnerResult.Value),
            CreatedAt: serializedInnerResult.CreatedAt,
            UpdatedAt: serializedInnerResult.UpdatedAt
        );
    }

    public static InnerResult<TId, TValue> Deserialize<TId, TValue>(
        this IInnerResultDefinition<TId, TValue> definition,
        SerializedInnerResult serializedInnerResult
    )
    {
        Debug.Assert(definition.Type == serializedInnerResult.Type);

        if (serializedInnerResult.Id is null)
        {
            throw new SerializationException($"inner result {definition.Type} has no id");
        }

        return new InnerResult<TId, TValue>(
            definition.Type,
            definition.DeserializeId(serializedInnerResult.Id!.Value),
            definition.DeserializeValue(serializedInnerResult.Value),
            CreatedAt: serializedInnerResult.CreatedAt,
            UpdatedAt: serializedInnerResult.UpdatedAt
        );
    }

    #endregion

    #region Id serialization

    public static SerializedInnerResultId SerializeId<TId, TValue>(
        this IInnerResultDefinition<TId, TValue> definition,
        TId id
    )
    {
        try
        {
            var serializedId = definition.IdSerializer.Serialize(id);
            return new SerializedInnerResultId(serializedId);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize id of inner result {definition.Type}",
                e
            );
        }
    }

    public static TId DeserializeId<TId, TValue>(
        this IInnerResultDefinition<TId, TValue> definition,
        SerializedInnerResultId serializedId
    )
    {
        try
        {
            return definition.IdSerializer.Deserialize(serializedId.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize id of inner result {definition.Type}",
                e
            );
        }
    }

    #endregion

    #region Value serialization

    public static SerializedInnerResultValue SerializeValue<TValue>(
        this IInnerResultDefinition<TValue> definition,
        TValue value
    )
    {
        try
        {
            var serializedResultValue = definition.ValueSerializer.Serialize(value);
            return new SerializedInnerResultValue(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inner result {definition.Type}",
                e
            );
        }
    }

    public static TValue DeserializeValue<TValue>(
        this IInnerResultDefinition<TValue> definition,
        SerializedInnerResultValue serializedValue
    )
    {
        try
        {
            return definition.ValueSerializer.Deserialize(serializedValue.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize value of inner result {definition.Type}",
                e
            );
        }
    }

    public static SerializedInnerResultValue SerializeValue<TKey, TValue>(
        this IInnerResultDefinition<TKey, TValue> definition,
        TValue value
    )
    {
        try
        {
            var serializedResultValue = definition.ValueSerializer.Serialize(value);
            return new SerializedInnerResultValue(serializedResultValue);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to serialize value of inner result {definition.Type}",
                e
            );
        }
    }

    public static TValue DeserializeValue<TKey, TValue>(
        this IInnerResultDefinition<TKey, TValue> definition,
        SerializedInnerResultValue serializedValue
    )
    {
        try
        {
            return definition.ValueSerializer.Deserialize(serializedValue.Value);
        }
        catch (Exception e)
        {
            throw new SerializationException(
                $"Failed to deserialize value of inner result {definition.Type}",
                e
            );
        }
    }

    #endregion
}
