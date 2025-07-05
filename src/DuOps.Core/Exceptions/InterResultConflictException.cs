namespace DuOps.Core.Exceptions;

public class InterResultConflictException : DuOpsException
{
    public InterResultConflictException() { }

    public InterResultConflictException(string message)
        : base(message) { }

    public InterResultConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
