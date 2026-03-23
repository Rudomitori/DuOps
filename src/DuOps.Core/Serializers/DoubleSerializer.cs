using System.Globalization;

namespace DuOps.Core.Serializers;

public sealed class DoubleSerializer : ISerializer<double>
{
    public static readonly DoubleSerializer Instance = new();

    public string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);

    public double Deserialize(string serialized) =>
        int.Parse(serialized, CultureInfo.InvariantCulture);
}
