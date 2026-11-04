using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Repositories;

/// <summary>
/// The only place that accesses the Requests and RequestStatusHistory tables.
/// Filtering, sorting and paging are composed as IQueryable and executed by SQL Server,
/// so only one page of rows is ever loaded into memory.
/// </summary>
public class RequestRepository : IRequestRepository
{
    private readonly RequestsDbContext _context;

    public RequestRepository(RequestsDbContext context)
    {
        _context = context;
    }

    // ---------- Read operations ----------

    public async Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestFilter filter,
        CancellationToken cancellationToken)
    {
        // Building the query only - nothing is sent to the database yet.
        var query = ApplyFilters(_context.Requests.AsNoTracking(), filter);

        // Total count of the filtered set, calculated in the database.
        var totalCount = await query.CountAsync(cancellationToken);

        // ORDER BY + OFFSET/FETCH in SQL: only one page is returned.
        var items = await ApplySorting(query, filter)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _context.Requests.AnyAsync(r => r.Id == id, cancellationToken);

    // GROUP BY in the database, covered by the (Status, CreatedAt) index.
    public async Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(CancellationToken cancellationToken) =>
        await _context.Requests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

    // GROUP BY in the database, covered by the (Priority, CreatedAt) index.
    public async Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(CancellationToken cancellationToken) =>
        await _context.Requests
            .AsNoTracking()
            .GroupBy(r => r.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

    // Covered by the (RequestId, ChangedAt) index.
    public async Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(
        int requestId,
        CancellationToken cancellationToken) =>
        await _context.RequestStatusHistory
            .AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);

    // ---------- Write operations ----------

    // Tracked: EF records the changes made to this entity and saves them later.
    public Task<Request?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    // The UPDATE will include "WHERE RowVersion = @expected".
    // If another user changed the row in the meantime, no row matches and EF reports a conflict.
    public void SetExpectedRowVersion(Request request, byte[] expectedRowVersion) =>
        _context.Entry(request).Property(r => r.RowVersion).OriginalValue = expectedRowVersion;

    public void AddHistory(RequestStatusHistory history) =>
        _context.RequestStatusHistory.Add(history);

    // SaveChanges runs the UPDATE and the audit INSERT in one transaction.
    // Returns false on a concurrency conflict; in that case nothing was saved.
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Discard the pending changes so they are not retried by a later save.
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    // ---------- Query helpers ----------

    private static IQueryable<Request> ApplyFilters(IQueryable<Request> query, RequestFilter filter)
    {
        if (filter.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(r => r.Priority == priority);
        }

        if (filter.OrganizationName is { } organization)
        {
            query = query.Where(r => r.OrganizationName == organization);
        }

        if (filter.AssignedTo is { } assignedTo)
        {
            query = query.Where(r => r.AssignedTo == assignedTo);
        }

        if (filter.CreatedFrom is { } from)
        {
            query = query.Where(r => r.CreatedAt >= from);
        }

        if (filter.CreatedToExclusive is { } toExclusive)
        {
            query = query.Where(r => r.CreatedAt < toExclusive);
        }

        if (filter.Search is { } term)
        {
            // Translated to LIKE '%term%'. Cannot use an index seek - see performance notes.
            query = query.Where(r => r.Title.Contains(term) || r.OrganizationName.Contains(term));
        }

        return query;
    }

    private static IQueryable<Request> ApplySorting(IQueryable<Request> query, RequestFilter filter)
    {
        var descending = filter.SortDirection == SortDirection.Desc;

        // Each sort matches an index exactly (including Id at the end),
        // so SQL Server reads the index in order instead of sorting 100,000 rows.
        IOrderedQueryable<Request> ordered = filter.SortBy switch
        {
            RequestSortField.UpdatedAt => OrderByDirection(query, r => r.UpdatedAt, descending),
            RequestSortField.Title => OrderByDirection(query, r => r.Title, descending),
            RequestSortField.OrganizationName => OrderByDirection(query, r => r.OrganizationName, descending),
            RequestSortField.Status => ThenByDirection(OrderByDirection(query, r => r.Status, descending), r => r.CreatedAt, descending),
            RequestSortField.Priority => ThenByDirection(OrderByDirection(query, r => r.Priority, descending), r => r.CreatedAt, descending),
            _ => OrderByDirection(query, r => r.CreatedAt, descending)
        };

        // Tie-breaker by Id: rows with equal sort values keep a stable order between pages.
        return ThenByDirection(ordered, r => r.Id, descending);
    }

    private static IOrderedQueryable<Request> OrderByDirection<TKey>(
        IQueryable<Request> query,
        Expression<Func<Request, TKey>> keySelector,
        bool descending) =>
        descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    private static IOrderedQueryable<Request> ThenByDirection<TKey>(
        IOrderedQueryable<Request> query,
        Expression<Func<Request, TKey>> keySelector,
        bool descending) =>
        descending ? query.ThenByDescending(keySelector) : query.ThenBy(keySelector);
}