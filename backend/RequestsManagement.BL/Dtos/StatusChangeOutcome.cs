namespace RequestsManagement.BL.Dtos;

/// <summary>
/// Result of a status change for a single request.
/// Used by the bulk endpoint to report what happened to each item.
/// </summary>
public enum StatusChangeOutcome
{
    Updated,
    NotFound,
    Conflict,
    InvalidTransition
}