using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;

namespace DuOps.Samples.WebApi.SampleOperation.InnerResults;

public sealed class AwaitExternalLongProcessInnerResultDefinition : IInnerResultDefinition<DateTime>
{
    public static readonly AwaitExternalLongProcessInnerResultDefinition Instance = new();
    public InnerResultType Type => new("AwaitExternalLongProcess");

    public ISerializer<DateTime> ValueSerializer => DateTimeSerializer.Instance;
}
