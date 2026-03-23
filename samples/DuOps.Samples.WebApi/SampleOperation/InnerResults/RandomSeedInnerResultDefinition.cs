using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;

namespace DuOps.Samples.WebApi.SampleOperation.InnerResults;

public sealed class RandomSeedInnerResultDefinition : IInnerResultDefinition<int>
{
    public static readonly RandomSeedInnerResultDefinition Instance = new();

    public InnerResultType Type => new("RandomSeed");

    public ISerializer<int> ValueSerializer => IntSerializer.Instance;
}
