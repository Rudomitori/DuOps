using System.Text.RegularExpressions;

namespace DuOps.Core.Operations.InterResults.Definitions;

public readonly partial record struct InterResultDiscriminator(string Value)
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
                $"InterResultDiscriminator must match regex \'{ValueRegex()}\'"
            );
        }

        return value;
    }

    public override string ToString() => Value;

    public static implicit operator string(InterResultDiscriminator discriminator) =>
        discriminator.Value;
}
