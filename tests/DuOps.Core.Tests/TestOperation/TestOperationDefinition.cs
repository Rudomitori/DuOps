using DuOps.Core.OperationDefinitions;
using DuOps.Core.Serializers;

namespace DuOps.Core.Tests.TestOperation;

public sealed class TestOperationDefinition
    : IOperationDefinition<Guid, TestOperationArgs, TestOperationResult>
{
    public static readonly TestOperationDefinition Instance = new();

    public OperationType Type { get; } = new("TestOperation");

    public ISerializer<Guid> IdSerializer => GuidSerializer.Instance;

    public ISerializer<TestOperationArgs> ArgsSerializer { get; } =
        JsonSerializer<TestOperationArgs>.Default;
    public ISerializer<TestOperationResult> ResultSerializer { get; } =
        JsonSerializer<TestOperationResult>.Default;
}
