using System.Text.RegularExpressions;

namespace DuOps.Core.Operations;

public readonly partial record struct OperationPollingScheduleId(string Value)
{
    public string Value { get; } =
        ValidateValue(Value ?? throw new ArgumentNullException(nameof(Value)));

    [GeneratedRegex(@"^\s|\s$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value) || ValueRegex().IsMatch(value))
        {
            throw new ArgumentException(
                "OperationPollingScheduleId must be trimmed not empty string"
            );
        }

        return value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OperationPollingScheduleId id) => id.Value;
}
