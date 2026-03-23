using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Tests.TestOperation.InnerResults.First;

public sealed class FirstInnerResultDefinition : IInnerResultDefinition<FirstInnerResultValue>
{
    public static readonly FirstInnerResultDefinition Instance = new();

    public InnerResultType Type { get; } = new("First");

    public ISerializer<FirstInnerResultValue> ValueSerializer =>
        JsonSerializer<FirstInnerResultValue>.Default;
}
