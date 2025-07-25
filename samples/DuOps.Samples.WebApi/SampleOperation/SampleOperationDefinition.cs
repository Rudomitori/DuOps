using System.Text.Json;
using DuOps.Core.OperationDefinitions;

namespace DuOps.Samples.WebApi.SampleOperation;

public sealed class SampleOperationDefinition
    : IOperationDefinition<SampleOperationArgs, SampleOperationResult>
{
    public static readonly SampleOperationDefinition Instance = new();

    public OperationDiscriminator Discriminator => new("Sample");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string SerializeArgs(SampleOperationArgs args) =>
        JsonSerializer.Serialize(args, JsonSerializerOptions);

    public SampleOperationArgs DeserializeArgs(string serializedArgs) =>
        JsonSerializer.Deserialize<SampleOperationArgs>(serializedArgs, JsonSerializerOptions)!;

    public string SerializeResult(SampleOperationResult result) =>
        JsonSerializer.Serialize(result, JsonSerializerOptions);

    public SampleOperationResult DeserializeResult(string serializedResult) =>
        JsonSerializer.Deserialize<SampleOperationResult>(serializedResult, JsonSerializerOptions)!;
}
