using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Repositories;

/// <summary>
/// Filter, sort and paging criteria for querying requests.
/// Built by the BL from validated input; the DAL only translates it to SQL.
/// </summary>
public record RequestFilter(
    RequestStatus? Status,
    RequestPriority? Priority,
    string? OrganizationName,
    string? AssignedTo,
    DateTime? CreatedFrom,
    DateTime? CreatedToExclusive,
    string? Search,
    RequestSortField SortBy,
    SortDirection SortDirection,
    int Skip,
    int Take);