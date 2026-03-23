namespace DuOps.Core.Serializers;

public sealed class GuidSerializer : ISerializer<Guid>
{
    public static readonly GuidSerializer Instance = new();

    public string Serialize(Guid value) => value.ToString();

    public Guid Deserialize(string serialized) => Guid.Parse(serialized);
}
