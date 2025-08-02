using System.Text.Json;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationDefinitions.RetryPolicies;

namespace DuOps.Core.Tests.TestOperation;

public sealed class TestOperationDefinition
    : IOperationDefinition<TestOperationArgs, TestOperationResult>
{
    public static readonly TestOperationDefinition Instance = new();

    public OperationDiscriminator Discriminator { get; } = new("TestOperation");

    public IOperationRetryPolicy RetryPolicy => ZeroOperationRetryingPolicy.Instance;

    public string SerializeArgs(TestOperationArgs args) => JsonSerializer.Serialize(args);

    public TestOperationArgs DeserializeArgs(string serializedArgs) =>
        JsonSerializer.Deserialize<TestOperationArgs>(serializedArgs)!;

    public string SerializeResult(TestOperationResult result) => JsonSerializer.Serialize(result);

    public TestOperationResult DeserializeResult(string serializedResult) =>
        JsonSerializer.Deserialize<TestOperationResult>(serializedResult)!;
}
