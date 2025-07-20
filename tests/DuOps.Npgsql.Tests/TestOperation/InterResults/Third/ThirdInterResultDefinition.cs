using System.Text.Json;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Npgsql.Tests.TestOperation.InterResults.Third;

public sealed class ThirdInterResultDefinition : IInterResultDefinition<int, Guid>
{
    public static readonly ThirdInterResultDefinition Instance = new();

    public InterResultDiscriminator Discriminator { get; } = new("Third");

    public string SerializeValue(Guid resultValue) => JsonSerializer.Serialize(resultValue);

    public Guid DeserializeValue(string serializedResult) =>
        JsonSerializer.Deserialize<Guid>(serializedResult)!;

    public string SerializeKey(int key) => JsonSerializer.Serialize(key);

    public int DeserializeKey(string serializedKey) =>
        JsonSerializer.Deserialize<int>(serializedKey);
}
