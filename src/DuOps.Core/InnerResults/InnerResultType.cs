using System.Text.RegularExpressions;

namespace DuOps.Core.InnerResults;

public sealed partial class InnerResultType(string value)
    : IEquatable<InnerResultType>,
        IComparable<InnerResultType>,
        IComparable
{
    public string Value { get; } =
        ValidateValue(value ?? throw new ArgumentNullException(nameof(value)));

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        return !ValueRegex().IsMatch(value)
            ? throw new ArgumentException($"InnerResultType must match regex \'{ValueRegex()}\'")
            : value;
    }

    public override string ToString() => Value;

    public static implicit operator string(InnerResultType type) => type.Value;

    #region Equals

    public static bool operator ==(InnerResultType? left, InnerResultType? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InnerResultType? left, InnerResultType? right)
    {
        return !Equals(left, right);
    }

    public bool Equals(InnerResultType? other)
    {
        return ReferenceEquals(this, other) || Value == other?.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is InnerResultType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.InvariantCulture.GetHashCode(Value);
    }

    #endregion

    #region Compare

    public int CompareTo(InnerResultType? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (other is null)
            return 1;

        return string.Compare(Value, other.Value, StringComparison.InvariantCulture);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return 0;

        if (obj is null)
            return 1;

        return obj is InnerResultType other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(InnerResultType)}");
    }

    public static bool operator <(InnerResultType? left, InnerResultType? right)
    {
        return Comparer<InnerResultType>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(InnerResultType? left, InnerResultType? right)
    {
        return Comparer<InnerResultType>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(InnerResultType? left, InnerResultType? right)
    {
        return Comparer<InnerResultType>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(InnerResultType? left, InnerResultType? right)
    {
        return Comparer<InnerResultType>.Default.Compare(left, right) >= 0;
    }

    #endregion
}
