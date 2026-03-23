using System.Globalization;

namespace DuOps.Core.Serializers;

public sealed class IntSerializer : ISerializer<int>
{
    public static readonly IntSerializer Instance = new();

    public string Serialize(int value) => value.ToString(CultureInfo.InvariantCulture);

    public int Deserialize(string serialized) =>
        int.Parse(serialized, CultureInfo.InvariantCulture);
}
