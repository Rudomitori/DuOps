using System.Globalization;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Samples.WebApi.SampleOperation.InterResults;

public sealed class RandomSeedInterResultDefinition : IInterResultDefinition<int>
{
    public static readonly RandomSeedInterResultDefinition Instance = new();

    public InterResultDiscriminator Discriminator => new("RandomSeed");

    public string SerializeValue(int result) => result.ToString(CultureInfo.InvariantCulture);

    public int DeserializeValue(string serializedResult) => int.Parse(serializedResult);
}
