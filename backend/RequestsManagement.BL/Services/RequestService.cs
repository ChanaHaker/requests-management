using Microsoft.Extensions.Logging;
using RequestsManagement.BL.Caching;
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
    private readonly IRequestStatisticsCache _statisticsCache;
    private readonly ILogger<RequestService> _logger;

    public RequestService(
        IRequestRepository repository,
        IRequestStatisticsCache statisticsCache,
        ILogger<RequestService> logger)
    {
        _repository = repository;
        _statisticsCache = statisticsCache;
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

    // Served from cache; recalculated only after expiration or a status change.
    public Task<RequestStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken) =>
        _statisticsCache.GetOrCreateAsync(CalculateStatisticsAsync, cancellationToken);

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
        var expectedRowVersion = Convert.FromBase64String(update.RowVersion);

        var result = await TryChangeStatusAsync(
            id, update.NewStatus!.Value, expectedRowVersion, changedBy, cancellationToken);

        // A single update reports failures as exceptions, mapped to HTTP codes in one place.
        return result.Outcome switch
        {
            StatusChangeOutcome.Updated => result.Request!.ToDto(),
            StatusChangeOutcome.NotFound => throw new NotFoundException(result.Message!),
            StatusChangeOutcome.Conflict => throw new ConflictException(result.Message!),
            _ => throw new BusinessRuleException(result.Message!)
        };
    }

    public async Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(
        BulkUpdateStatusRequest bulk,
        string changedBy,
        CancellationToken cancellationToken)
    {
        var newStatus = bulk.NewStatus!.Value;
        var results = new List<BulkUpdateItemResult>(bulk.Items.Count);

        // Partial success: each item is loaded, checked and saved in its own transaction,
        // so a missing or conflicting request does not block the others.
        foreach (var item in bulk.Items)
        {
            var expectedRowVersion = Convert.FromBase64String(item.RowVersion);

            var result = await TryChangeStatusAsync(
                item.Id, newStatus, expectedRowVersion, changedBy, cancellationToken);

            var newRowVersion = result.Outcome == StatusChangeOutcome.Updated
                ? Convert.ToBase64String(result.Request!.RowVersion)
                : null;

            results.Add(new BulkUpdateItemResult(item.Id, result.Outcome, result.Message, newRowVersion));
        }

        var updatedCount = results.Count(r => r.Outcome == StatusChangeOutcome.Updated);

        _logger.LogInformation(
            "Bulk status update to {NewStatus}: {Updated} of {Requested} requests updated.",
            newStatus, updatedCount, results.Count);

        return new BulkUpdateStatusResult(results.Count, updatedCount, results.Count - updatedCount, results);
    }

    /// <summary>
    /// The status change logic shared by the single and bulk updates.
    /// Returns an outcome instead of throwing, so the bulk update can continue with the next item.
    /// </summary>
    private async Task<StatusChangeResult> TryChangeStatusAsync(
        int id,
        RequestStatus newStatus,
        byte[] expectedRowVersion,
        string changedBy,
        CancellationToken cancellationToken)
    {
        // 1. Load the request (tracked, so changes can be saved).
        var request = await _repository.GetForUpdateAsync(id, cancellationToken);
        if (request is null)
        {
            return StatusChangeResult.Failed(StatusChangeOutcome.NotFound, $"Request {id} was not found.");
        }

        // 2. Early version check: the client must have seen the current version.
        //    Checked before the business rules, so the user is asked to reload
        //    instead of getting a rule error about a status they never saw.
        if (!request.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return StatusChangeResult.Failed(StatusChangeOutcome.Conflict, ConflictMessage(id));
        }

        // 3. Business rule: only allowed transitions.
        var previousStatus = request.Status;
        if (!StatusTransitions.IsAllowed(previousStatus, newStatus))
        {
            var allowed = StatusTransitions.GetAllowedTargets(previousStatus);
            var allowedText = allowed.Count == 0 ? "none (final status)" : string.Join(", ", allowed);

            return StatusChangeResult.Failed(
                StatusChangeOutcome.InvalidTransition,
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
            return StatusChangeResult.Failed(StatusChangeOutcome.Conflict, ConflictMessage(id));
        }

        // The status counts changed, so the cached statistics are no longer valid.
        _statisticsCache.Invalidate();

        _logger.LogInformation(
            "Request {RequestId} status changed from {PreviousStatus} to {NewStatus}.",
            id, previousStatus, newStatus);

        // 7. The entity now holds the new RowVersion generated by the database.
        return StatusChangeResult.Succeeded(request);
    }

    // ---------- Helpers ----------

    private async Task<RequestStatisticsDto> CalculateStatisticsAsync(CancellationToken cancellationToken)
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

    private static string ConflictMessage(int id) =>
        $"Request {id} was modified by another user. Reload it and try again.";

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

    private sealed record StatusChangeResult(StatusChangeOutcome Outcome, Request? Request, string? Message)
    {
        public static StatusChangeResult Succeeded(Request request) =>
            new(StatusChangeOutcome.Updated, request, null);

        public static StatusChangeResult Failed(StatusChangeOutcome outcome, string message) =>
            new(outcome, null, message);
    }
}