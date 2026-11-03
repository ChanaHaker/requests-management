using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Dtos;

public record RequestDto(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);