using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Samples.WebApi.SampleOperation.InterResults;

public sealed class AwaitExternalLongProcessInterResultDefinition : IInterResultDefinition<DateTime>
{
    public static readonly AwaitExternalLongProcessInterResultDefinition Instance = new();
    public InterResultDiscriminator Discriminator => new("AwaitExternalLongProcess");

    public string SerializeValue(DateTime result) => result.ToString("O");

    public DateTime DeserializeValue(string serializedResult) => DateTime.Parse(serializedResult);
}
