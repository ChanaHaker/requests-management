using RequestsManagement.BL.Dtos;
using RequestsManagement.BL.Exceptions;
using RequestsManagement.BL.Mapping;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DAL.Repositories;

namespace RequestsManagement.BL.Services;

/// <summary>
/// Business logic for requests. Has no knowledge of EF Core or the database:
/// it translates validated input into DAL criteria, maps entities to DTOs
/// and applies business rules.
/// </summary>
public class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<RequestDto>> GetRequestsAsync(
        RequestQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var filter = ToFilter(parameters);

        var (items, totalCount) = await _repository.GetPagedAsync(filter, cancellationToken);

        return new PagedResult<RequestDto>
        {
            Items = items.Select(r => r.ToDto()).ToList(),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RequestDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Request {id} was not found.");

        return request.ToDto();
    }

    public async Task<RequestStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var statusCounts = await _repository.CountByStatusAsync(cancellationToken);
        var priorityCounts = await _repository.CountByPriorityAsync(cancellationToken);

        // Include every value, with 0 when no request has it.
        var byStatus = Enum.GetValues<RequestStatus>()
            .ToDictionary(s => s, s => statusCounts.GetValueOrDefault(s));

        var byPriority = Enum.GetValues<RequestPriority>()
            .ToDictionary(p => p, p => priorityCounts.GetValueOrDefault(p));

        return new RequestStatisticsDto(byStatus.Values.Sum(), byStatus, byPriority);
    }

    private static RequestFilter ToFilter(RequestQueryParameters parameters) =>
        new(
            Status: parameters.Status,
            Priority: parameters.Priority,
            OrganizationName: Normalize(parameters.OrganizationName),
            AssignedTo: Normalize(parameters.AssignedTo),
            CreatedFrom: parameters.CreatedFrom?.Date,
            // For the user the end date is inclusive, so the whole day is included.
            CreatedToExclusive: parameters.CreatedTo?.Date.AddDays(1),
            Search: Normalize(parameters.Search),
            SortBy: parameters.SortBy,
            SortDirection: parameters.SortDirection,
            // Calculated as long to avoid overflow for very large page numbers.
            Skip: (int)Math.Min((long)(parameters.Page - 1) * parameters.PageSize, int.MaxValue),
            Take: parameters.PageSize);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}