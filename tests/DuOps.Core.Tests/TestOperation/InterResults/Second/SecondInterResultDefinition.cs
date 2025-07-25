using System.Text.Json;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Tests.TestOperation.InterResults.Second;

public sealed class SecondInterResultDefinition : IInterResultDefinition<double>
{
    public static readonly SecondInterResultDefinition Instance = new();

    public InterResultDiscriminator Discriminator { get; } = new("Second");

    public string SerializeValue(double resultValue) => JsonSerializer.Serialize(resultValue);

    public double DeserializeValue(string serializedResult) =>
        JsonSerializer.Deserialize<double>(serializedResult)!;
}
