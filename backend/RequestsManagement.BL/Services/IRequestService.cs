using RequestsManagement.BL.Dtos;

namespace RequestsManagement.BL.Services;

public interface IRequestService
{
    Task<PagedResult<RequestDto>> GetRequestsAsync(
        RequestQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<RequestDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RequestStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken cancellationToken);

    Task<RequestDto> UpdateStatusAsync(
        int id,
        UpdateStatusRequest update,
        string changedBy,
        CancellationToken cancellationToken);

    Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(
        BulkUpdateStatusRequest bulk,
        string changedBy,
        CancellationToken cancellationToken);
}