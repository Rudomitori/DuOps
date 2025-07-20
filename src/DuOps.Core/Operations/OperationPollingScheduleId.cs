namespace DuOps.Core.Operations;

// TODO: Disallow not trimmed and whitespace Value
// TODO: Add null check
public readonly record struct OperationPollingScheduleId(string Value)
{
    public override string ToString() => Value;
}
