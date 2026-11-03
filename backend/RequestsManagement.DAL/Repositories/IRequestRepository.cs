using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Repositories;

public interface IRequestRepository
{
    // Read operations (no tracking)
    Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestFilter filter,
        CancellationToken cancellationToken);

    Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);

    // Write operations
    Task<Request?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    void SetExpectedRowVersion(Request request, byte[] expectedRowVersion);

    void AddHistory(RequestStatusHistory history);

    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}