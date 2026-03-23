namespace DuOps.Core.Exceptions;

public sealed class WaitException : DuOpsException
{
    public string Reason { get; }

    public TimeSpan? Duration { get; }

    public DateTime? Until { get; }

    public WaitException(string reason, TimeSpan duration)
        : base($"Operation waiting {duration:c} because {reason}")
    {
        Reason = reason;
        Duration = duration;
    }

    public WaitException(string reason, DateTime until)
        : base($"Operation waiting until {until:u} because {reason}")
    {
        Reason = reason;
        Until = until;
    }
}
