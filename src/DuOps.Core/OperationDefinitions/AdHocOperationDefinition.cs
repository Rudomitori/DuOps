namespace DuOps.Core.OperationDefinitions;

public sealed class AdHocOperationDefinition<TArgs, TResult> : IOperationDefinition<TArgs, TResult>
{
    public OperationDiscriminator Discriminator { get; }

    private readonly Func<TArgs, string> _serializeArgs;
    private readonly Func<string, TArgs> _deserializeArgs;

    private readonly Func<TResult, string> _serializeResult;
    private readonly Func<string, TResult> _deserializeResult;

    public AdHocOperationDefinition(
        OperationDiscriminator discriminator,
        Func<TArgs, string> serializeArgs,
        Func<string, TArgs> deserializeArgs,
        Func<TResult, string> serializeResult,
        Func<string, TResult> deserializeResult
    )
    {
        Discriminator = discriminator;
        _serializeArgs = serializeArgs;
        _deserializeArgs = deserializeArgs;
        _serializeResult = serializeResult;
        _deserializeResult = deserializeResult;
    }

    public string SerializeArgs(TArgs args) => _serializeArgs(args);

    public TArgs DeserializeArgs(string serializedArgs) => _deserializeArgs(serializedArgs);

    public string SerializeResult(TResult result) => _serializeResult(result);

    public TResult DeserializeResult(string serializedResult) =>
        _deserializeResult(serializedResult);
}
