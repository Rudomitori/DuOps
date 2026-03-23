using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Tests.TestOperation.InnerResults.Second;

public sealed class SecondInnerResultDefinition : IInnerResultDefinition<double>
{
    public static readonly SecondInnerResultDefinition Instance = new();

    public InnerResultType Type { get; } = new("Second");

    public ISerializer<double> ValueSerializer => DoubleSerializer.Instance;
}
