using System.Text.RegularExpressions;

namespace DuOps.Core.OperationDefinitions;

public readonly partial record struct OperationDiscriminator(string Value)
{
    public string Value { get; } =
        ValidateValue(Value ?? throw new ArgumentNullException(nameof(Value)));

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        if (!ValueRegex().IsMatch(value))
        {
            throw new ArgumentException(
                $"OperationDiscriminator must match regex \'{ValueRegex()}\'"
            );
        }

        return value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OperationDiscriminator discriminator) =>
        discriminator.Value;
}
