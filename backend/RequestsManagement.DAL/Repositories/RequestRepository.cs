using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Repositories;

/// <summary>
/// The only place that queries the Requests table.
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

        var ordered = filter.SortBy switch
        {
            RequestSortField.UpdatedAt => OrderByDirection(query, r => r.UpdatedAt, descending),
            RequestSortField.Priority => OrderByDirection(query, r => r.Priority, descending),
            RequestSortField.Status => OrderByDirection(query, r => r.Status, descending),
            RequestSortField.Title => OrderByDirection(query, r => r.Title, descending),
            RequestSortField.OrganizationName => OrderByDirection(query, r => r.OrganizationName, descending),
            _ => OrderByDirection(query, r => r.CreatedAt, descending)
        };

        // Tie-breaker by Id: rows with equal sort values keep a stable order between pages.
        return descending ? ordered.ThenByDescending(r => r.Id) : ordered.ThenBy(r => r.Id);
    }

    private static IOrderedQueryable<Request> OrderByDirection<TKey>(
        IQueryable<Request> query,
        Expression<Func<Request, TKey>> keySelector,
        bool descending) =>
        descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
}