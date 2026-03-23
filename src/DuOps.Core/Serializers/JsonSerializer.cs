using System.Text.Json;
using System.Text.Json.Serialization;

namespace DuOps.Core.Serializers;

public sealed class JsonSerializer<TValue>(JsonSerializerOptions options) : ISerializer<TValue>
{
    public static readonly JsonSerializer<TValue> Default = new(
        AdHocJsonSerializerConstants.DefaultOptions
    );

    public string Serialize(TValue value) => JsonSerializer.Serialize(value, options);

    public TValue Deserialize(string serialized) =>
        JsonSerializer.Deserialize<TValue>(serialized, options)!;
}

internal static class AdHocJsonSerializerConstants
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
