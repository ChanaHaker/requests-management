namespace RequestsManagement.BL.Dtos;

/// <summary>
/// Summary and per-item results of a bulk status update.
/// </summary>
public record BulkUpdateStatusResult(
    int Requested,
    int Updated,
    int Failed,
    IReadOnlyList<BulkUpdateItemResult> Results);