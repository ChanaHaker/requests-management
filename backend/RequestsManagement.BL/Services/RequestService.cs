using Microsoft.Extensions.Logging;
using RequestsManagement.BL.Dtos;
using RequestsManagement.BL.Exceptions;
using RequestsManagement.BL.Mapping;
using RequestsManagement.BL.Validation;
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
    private readonly ILogger<RequestService> _logger;

    public RequestService(IRequestRepository repository, ILogger<RequestService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ---------- Read ----------

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

    public async Task<IReadOnlyList<RequestStatusHistoryDto>> GetHistoryAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await _repository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException($"Request {id} was not found.");
        }

        var history = await _repository.GetHistoryAsync(id, cancellationToken);
        return history.Select(h => h.ToDto()).ToList();
    }

    // ---------- Write ----------

    public async Task<RequestDto> UpdateStatusAsync(
        int id,
        UpdateStatusRequest update,
        string changedBy,
        CancellationToken cancellationToken)
    {
        // 1. Load the request (tracked, so changes can be saved).
        var request = await _repository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Request {id} was not found.");

        // 2. Early version check: the client must have seen the current version.
        //    Checked before the business rules, so the user is asked to reload
        //    instead of getting a rule error about a status they never saw.
        var expectedRowVersion = Convert.FromBase64String(update.RowVersion);
        if (!request.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConflictException(
                $"Request {id} was modified by another user. Reload it and try again.");
        }

        // 3. Business rule: only allowed transitions.
        var newStatus = update.NewStatus!.Value;
        var previousStatus = request.Status;

        if (!StatusTransitions.IsAllowed(previousStatus, newStatus))
        {
            var allowed = StatusTransitions.GetAllowedTargets(previousStatus);
            var allowedText = allowed.Count == 0 ? "none (final status)" : string.Join(", ", allowed);

            throw new BusinessRuleException(
                $"Cannot change status from {previousStatus} to {newStatus}. Allowed: {allowedText}.");
        }

        // 4. Apply the change.
        var now = DateTime.UtcNow;
        request.Status = newStatus;
        request.UpdatedAt = now;

        // 5. Audit row - saved in the same transaction as the update.
        _repository.AddHistory(new RequestStatusHistory
        {
            RequestId = request.Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedBy = changedBy
        });

        // 6. Save. The database enforces the version check (WHERE RowVersion = expected),
        //    which also covers two updates arriving at exactly the same moment.
        _repository.SetExpectedRowVersion(request, expectedRowVersion);

        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            throw new ConflictException(
                $"Request {id} was modified by another user. Reload it and try again.");
        }

        _logger.LogInformation(
            "Request {RequestId} status changed from {PreviousStatus} to {NewStatus}.",
            id, previousStatus, newStatus);

        // 7. The entity now holds the new RowVersion generated by the database.
        return request.ToDto();
    }

    // ---------- Helpers ----------

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