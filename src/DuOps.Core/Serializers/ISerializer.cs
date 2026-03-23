namespace DuOps.Core.Serializers;

public interface ISerializer<TValue>
{
    string Serialize(TValue value);
    TValue Deserialize(string serialized);
}
