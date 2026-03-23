namespace DuOps.Core.Serializers;

public sealed class NullSerializer : ISerializer<object?>
{
    public static readonly NullSerializer Instance = new();

    public string Serialize(object? value) => "";

    public object? Deserialize(string serialized) => null;
}
