using System.Text.RegularExpressions;

namespace DuOps.Core.OperationDefinitions;

public sealed partial class OperationType(string value)
    : IEquatable<OperationType>,
        IComparable<OperationType>,
        IComparable
{
    public string Value { get; } =
        ValidateValue(value ?? throw new ArgumentNullException(nameof(value)));

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        return !ValueRegex().IsMatch(value)
            ? throw new ArgumentException($"OperationType must match regex \'{ValueRegex()}\'")
            : value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OperationType type) => type.Value;

    #region Equals

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(OperationType? other)
    {
        return ReferenceEquals(this, other) || Value == other?.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is OperationType other && Equals(other);
    }

    public static bool operator ==(OperationType? left, OperationType? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationType? left, OperationType? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Compare

    public static bool operator <(OperationType? left, OperationType? right)
    {
        return Comparer<OperationType>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(OperationType? left, OperationType? right)
    {
        return Comparer<OperationType>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(OperationType? left, OperationType? right)
    {
        return Comparer<OperationType>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(OperationType? left, OperationType? right)
    {
        return Comparer<OperationType>.Default.Compare(left, right) >= 0;
    }

    public int CompareTo(OperationType? other)
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

        return obj is OperationType other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(OperationType)}");
    }

    #endregion
}
