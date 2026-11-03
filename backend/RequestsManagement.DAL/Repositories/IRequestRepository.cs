using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Repositories;

public interface IRequestRepository
{
    Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestFilter filter,
        CancellationToken cancellationToken);

    Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(CancellationToken cancellationToken);
}