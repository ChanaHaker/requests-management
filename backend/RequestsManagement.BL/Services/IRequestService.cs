using RequestsManagement.BL.Dtos;

namespace RequestsManagement.BL.Services;

public interface IRequestService
{
    Task<PagedResult<RequestDto>> GetRequestsAsync(
        RequestQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<RequestDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RequestStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken);
}