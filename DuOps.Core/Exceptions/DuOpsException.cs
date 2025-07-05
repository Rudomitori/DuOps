namespace DuOps.Core.Exceptions;

public abstract class DuOpsException : Exception
{
    public DuOpsException()
    {
    }

    public DuOpsException(
        string message
    ) : base(message)
    {
    }

    public DuOpsException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}
