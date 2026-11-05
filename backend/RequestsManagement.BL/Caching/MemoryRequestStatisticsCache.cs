using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RequestsManagement.BL.Dtos;

namespace RequestsManagement.BL.Caching;

/// <summary>
/// In-memory cache for the statistics (single server instance, registered as Singleton).
/// - Expiration: 5 minutes, as a safety net for changes made outside the API.
/// - Invalidation: called after every successful status change.
/// - Stampede protection: when the cache is empty, only one request calculates;
///   concurrent requests wait and reuse its result.
/// - A calculation that overlapped an invalidation is not cached, so stale counts are never stored.
/// </summary>
public class MemoryRequestStatisticsCache : IRequestStatisticsCache
{
    private const string CacheKey = "requests:statistics";
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryRequestStatisticsCache> _logger;

    // Allows a single calculation at a time.
    private readonly SemaphoreSlim _calculationLock = new(1, 1);

    // Incremented on every invalidation.
    private long _version;

    public MemoryRequestStatisticsCache(IMemoryCache cache, ILogger<MemoryRequestStatisticsCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<RequestStatisticsDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestStatisticsDto>> factory,
        CancellationToken cancellationToken)
    {
        // Fast path: no locking when the value is cached.
        if (_cache.TryGetValue(CacheKey, out RequestStatisticsDto? cached) && cached is not null)
        {
            _logger.LogDebug("Statistics served from cache.");
            return cached;
        }

        await _calculationLock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have calculated it while this one was waiting.
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
            {
                _logger.LogDebug("Statistics served from cache (calculated by a concurrent request).");
                return cached;
            }

            _logger.LogInformation("Statistics cache miss - calculating from the database.");

            var versionBefore = Interlocked.Read(ref _version);
            var statistics = await factory(cancellationToken);

            // If a status changed during the calculation, the result may already be stale: do not cache it.
            if (Interlocked.Read(ref _version) == versionBefore)
            {
                _cache.Set(CacheKey, statistics, Expiration);
            }
            else
            {
                _logger.LogInformation("Statistics changed during calculation - result not cached.");
            }

            return statistics;
        }
        finally
        {
            _calculationLock.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        _cache.Remove(CacheKey);
        _logger.LogInformation("Statistics cache invalidated.");
    }
}