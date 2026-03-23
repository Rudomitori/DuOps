using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Tests.TestOperation.InnerResults.Third;

public sealed class ThirdInnerResultDefinition : IInnerResultDefinition<int, Guid>
{
    public static readonly ThirdInnerResultDefinition Instance = new();

    public InnerResultType Type { get; } = new("Third");

    public ISerializer<int> IdSerializer => IntSerializer.Instance;
    public ISerializer<Guid> ValueSerializer => GuidSerializer.Instance;
}
