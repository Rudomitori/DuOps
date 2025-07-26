namespace DuOps.Core.Exceptions;

public sealed class YieldException : DuOpsException
{
    public string Reason { get; }
    public string? ReasonDetails { get; }

    public YieldException(string reason, string? reasonDetails)
        : base($"Operation yielded, because {reason}")
    {
        Reason = reason;
        ReasonDetails = reasonDetails;
    }
}
