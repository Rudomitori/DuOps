namespace DuOps.Core.Operations;

public readonly record struct OperationPollingScheduleId(
    string Value
)
{
    public override string ToString() => Value;
}
