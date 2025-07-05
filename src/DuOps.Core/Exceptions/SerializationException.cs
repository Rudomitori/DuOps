namespace DuOps.Core.Exceptions;

public sealed class SerializationException : DuOpsException
{
    public SerializationException() { }

    public SerializationException(string message)
        : base(message) { }

    public SerializationException(string message, Exception innerException)
        : base(message, innerException) { }
}
