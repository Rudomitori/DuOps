namespace DuOps.Core.Operations;

// TODO: Disallow not trimmed ShardKey and Value
// TODO: Disallow empty ShardKey and Value
// TODO: Extract ShardKey into separate value type
public readonly record struct OperationId(string? ShardKey, string Value)
{
    public static OperationId NewGuid() => new(null, Guid.NewGuid().ToString());

    public static OperationId NewGuid(string sharkKey) => new(sharkKey, Guid.NewGuid().ToString());

    public override string ToString()
    {
        if (ShardKey is null)
        {
            return Value;
        }

        return $"{ShardKey}|{Value}";
    }
}
