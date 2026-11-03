using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RequestsManagement.BL.Dtos;

namespace RequestsManagement.BL.Caching;

/// <summary>
/// In-memory cache for the statistics (single server instance).
/// - Expiration: 5 minutes, as a safety net for changes made outside the API.
/// - Invalidation: called after every successful status change.
/// </summary>
public class MemoryRequestStatisticsCache : IRequestStatisticsCache
{
    private const string CacheKey = "requests:statistics";
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryRequestStatisticsCache> _logger;

    public MemoryRequestStatisticsCache(IMemoryCache cache, ILogger<MemoryRequestStatisticsCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<RequestStatisticsDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestStatisticsDto>> factory,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out RequestStatisticsDto? cached) && cached is not null)
        {
            _logger.LogDebug("Statistics served from cache.");
            return cached;
        }

        _logger.LogInformation("Statistics cache miss - calculating from the database.");

        var statistics = await factory(cancellationToken);
        _cache.Set(CacheKey, statistics, Expiration);

        return statistics;
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Statistics cache invalidated.");
    }
}