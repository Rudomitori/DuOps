using System.Text.Json;
using DuOps.Core.OperationDefinitions;

namespace DuOps.Npgsql.Tests.TestOperation;

public sealed class TestOperationDefinition
    : IOperationDefinition<TestOperationArgs, TestOperationResult>
{
    public static readonly TestOperationDefinition Instance = new();

    public OperationDiscriminator Discriminator { get; } = new("TestOperation");

    public string SerializeArgs(TestOperationArgs args) => JsonSerializer.Serialize(args);

    public TestOperationArgs DeserializeArgs(string serializedArgs) =>
        JsonSerializer.Deserialize<TestOperationArgs>(serializedArgs)!;

    public string SerializeResult(TestOperationResult result) => JsonSerializer.Serialize(result);

    public TestOperationResult DeserializeResult(string serializedResult) =>
        JsonSerializer.Deserialize<TestOperationResult>(serializedResult)!;
}
