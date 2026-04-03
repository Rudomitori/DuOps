using System.Text.RegularExpressions;

namespace DuOps.Core.Storages;

public sealed partial class OperationStorageId(string value)
    : IEquatable<OperationStorageId>,
        IComparable<OperationStorageId>,
        IComparable
{
    public string Value { get; } =
        ValidateValue(value ?? throw new ArgumentNullException(nameof(value)));

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        return !ValueRegex().IsMatch(value)
            ? throw new ArgumentException($"OperationStorageId must match regex \'{ValueRegex()}\'")
            : value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OperationStorageId type) => type.Value;

    #region Equals

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(OperationStorageId? other)
    {
        return ReferenceEquals(this, other) || Value == other?.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is OperationStorageId other && Equals(other);
    }

    public static bool operator ==(OperationStorageId? left, OperationStorageId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationStorageId? left, OperationStorageId? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Compare

    public static bool operator <(OperationStorageId? left, OperationStorageId? right)
    {
        return Comparer<OperationStorageId>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(OperationStorageId? left, OperationStorageId? right)
    {
        return Comparer<OperationStorageId>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(OperationStorageId? left, OperationStorageId? right)
    {
        return Comparer<OperationStorageId>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(OperationStorageId? left, OperationStorageId? right)
    {
        return Comparer<OperationStorageId>.Default.Compare(left, right) >= 0;
    }

    public int CompareTo(OperationStorageId? other)
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

        return obj is OperationStorageId other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(OperationStorageId)}");
    }

    #endregion
}
