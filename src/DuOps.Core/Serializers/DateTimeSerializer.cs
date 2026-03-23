namespace DuOps.Core.Serializers;

public sealed class DateTimeSerializer : ISerializer<DateTime>
{
    public static readonly DateTimeSerializer Instance = new();

    public string Serialize(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime.Kind must be UTC");
        }

        return value.ToString("O");
    }

    public DateTime Deserialize(string serialized) => DateTime.Parse(serialized);
}
