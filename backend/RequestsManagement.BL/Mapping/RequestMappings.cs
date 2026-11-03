using RequestsManagement.BL.Dtos;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Mapping;

public static class RequestMappings
{
    public static RequestDto ToDto(this Request request) =>
        new(
            request.Id,
            request.Title,
            request.OrganizationName,
            request.Status,
            request.Priority,
            request.AssignedTo,
            request.CreatedAt,
            request.UpdatedAt,
            Convert.ToBase64String(request.RowVersion));
}