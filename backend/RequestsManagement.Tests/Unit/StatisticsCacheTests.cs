using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using RequestsManagement.BL.Caching;
using RequestsManagement.BL.Dtos;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.Tests.Unit;

/// <summary>
/// Statistics cache behavior: stampede protection and invalidation (no database involved).
/// </summary>
public class StatisticsCacheTests
{
    [Fact]
    public async Task ConcurrentRequestsOnEmptyCache_CalculateOnlyOnce()
    {
        var cache = CreateCache();
        var calculations = 0;

        async Task<RequestStatisticsDto> Calculate(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calculations);
            await Task.Delay(100, cancellationToken);
            return EmptyStatistics();
        }

        // 20 requests arrive at the same time while the cache is empty.
        var results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => cache.GetOrCreateAsync(Calculate, CancellationToken.None)));

        Assert.Equal(1, calculations);
        Assert.All(results, result => Assert.Same(results[0], result));
    }

    [Fact]
    public async Task Invalidate_ForcesRecalculation()
    {
        var cache = CreateCache();
        var calculations = 0;

        Task<RequestStatisticsDto> Calculate(CancellationToken cancellationToken)
        {
            calculations++;
            return Task.FromResult(EmptyStatistics());
        }

        await cache.GetOrCreateAsync(Calculate, CancellationToken.None);
        await cache.GetOrCreateAsync(Calculate, CancellationToken.None);   // served from cache
        cache.Invalidate();
        await cache.GetOrCreateAsync(Calculate, CancellationToken.None);   // recalculated

        Assert.Equal(2, calculations);
    }

    [Fact]
    public async Task InvalidateDuringCalculation_DoesNotCacheStaleResult()
    {
        var cache = CreateCache();
        var calculations = 0;
        var releaseFirstCalculation = new TaskCompletionSource();

        async Task<RequestStatisticsDto> Calculate(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref calculations) == 1)
            {
                // The first calculation is still running when a status changes.
                await releaseFirstCalculation.Task;
            }
            return EmptyStatistics();
        }

        var first = cache.GetOrCreateAsync(Calculate, CancellationToken.None);
        cache.Invalidate();
        releaseFirstCalculation.SetResult();
        await first;

        // The first result overlapped the invalidation, so it was not cached.
        await cache.GetOrCreateAsync(Calculate, CancellationToken.None);

        Assert.Equal(2, calculations);
    }

    private static MemoryRequestStatisticsCache CreateCache() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryRequestStatisticsCache>.Instance);

    private static RequestStatisticsDto EmptyStatistics() =>
        new(0, new Dictionary<RequestStatus, int>(), new Dictionary<RequestPriority, int>());
}