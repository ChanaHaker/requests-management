using RequestsManagement.BL.Dtos;

namespace RequestsManagement.BL.Caching;

/// <summary>
/// Cache for the request statistics.
/// Behind an interface so a distributed implementation (e.g. Redis)
/// can replace the in-memory one when running several server instances.
/// </summary>
public interface IRequestStatisticsCache
{
    Task<RequestStatisticsDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestStatisticsDto>> factory,
        CancellationToken cancellationToken);

    void Invalidate();
}