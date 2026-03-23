using System.Text.RegularExpressions;

namespace DuOps.Core;

public sealed partial class OperationQueueId(string value)
    : IEquatable<OperationQueueId>,
        IComparable<OperationQueueId>,
        IComparable
{
    public string Value { get; } =
        ValidateValue(value ?? throw new ArgumentNullException(nameof(value)));

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex ValueRegex();

    private static string ValidateValue(string value)
    {
        return !ValueRegex().IsMatch(value)
            ? throw new ArgumentException($"OperationQueueId must match regex \'{ValueRegex()}\'")
            : value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OperationQueueId type) => type.Value;

    #region Equals

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(OperationQueueId? other)
    {
        return ReferenceEquals(this, other) || Value == other?.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is OperationQueueId other && Equals(other);
    }

    public static bool operator ==(OperationQueueId? left, OperationQueueId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationQueueId? left, OperationQueueId? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Compare

    public static bool operator <(OperationQueueId? left, OperationQueueId? right)
    {
        return Comparer<OperationQueueId>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(OperationQueueId? left, OperationQueueId? right)
    {
        return Comparer<OperationQueueId>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(OperationQueueId? left, OperationQueueId? right)
    {
        return Comparer<OperationQueueId>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(OperationQueueId? left, OperationQueueId? right)
    {
        return Comparer<OperationQueueId>.Default.Compare(left, right) >= 0;
    }

    public int CompareTo(OperationQueueId? other)
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

        return obj is OperationQueueId other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(OperationQueueId)}");
    }

    #endregion
}
