namespace RequestsManagement.BL.Exceptions;

/// <summary>
/// Thrown when an operation breaks a business rule,
/// for example a forbidden status transition. Mapped to HTTP 400.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}