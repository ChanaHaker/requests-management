namespace RequestsManagement.BL.Dtos;

/// <summary>
/// What happened to one request in a bulk update.
/// NewRowVersion is returned only when the update succeeded.
/// </summary>
public record BulkUpdateItemResult(
    int Id,
    StatusChangeOutcome Outcome,
    string? Message,
    string? NewRowVersion);