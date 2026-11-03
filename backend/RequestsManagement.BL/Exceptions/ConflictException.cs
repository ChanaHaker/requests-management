namespace RequestsManagement.BL.Exceptions;

/// <summary>
/// Thrown when an update is based on a stale version of the data
/// (optimistic concurrency). Mapped to HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}