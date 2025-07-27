using System.Text.RegularExpressions;

namespace DuOps.Core.Operations;

public readonly partial record struct OperationId(string Value)
{
    public string Value { get; } =
        ValidateValue(Value ?? throw new ArgumentNullException(nameof(Value)));

    [GeneratedRegex(@"^\s|\s$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value) || ValueRegex().IsMatch(value))
        {
            throw new ArgumentException("OperationId must be trimmed not empty string");
        }

        return value;
    }

    public static OperationId NewGuid() => new(Guid.NewGuid().ToString());

    public static OperationId NewUuidV7() => new(Guid.CreateVersion7().ToString());

    public override string ToString() => Value;
}
