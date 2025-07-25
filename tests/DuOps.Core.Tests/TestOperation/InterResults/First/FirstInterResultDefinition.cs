using System.Text.Json;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Tests.TestOperation.InterResults.First;

public sealed class FirstInterResultDefinition : IInterResultDefinition<FirstInterResultValue>
{
    public static readonly FirstInterResultDefinition Instance = new();

    public InterResultDiscriminator Discriminator { get; } = new("First");

    public string SerializeValue(FirstInterResultValue resultValue) =>
        JsonSerializer.Serialize(resultValue);

    public FirstInterResultValue DeserializeValue(string serializedResult) =>
        JsonSerializer.Deserialize<FirstInterResultValue>(serializedResult)!;
}
