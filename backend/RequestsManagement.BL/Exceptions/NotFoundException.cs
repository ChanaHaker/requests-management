namespace RequestsManagement.BL.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist. Mapped to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}