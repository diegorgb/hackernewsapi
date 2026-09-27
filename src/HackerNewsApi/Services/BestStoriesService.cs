using HackerNewsApi.Clients;
using HackerNewsApi.Configuration;
using HackerNewsApi.Mapping;
using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Services;

/// <summary>
/// Fetches best stories from Hacker News and caches the fully materialised, score-sorted list
/// so concurrent callers can be served without repeatedly hitting the upstream API.
/// </summary>
public sealed class BestStoriesService : IBestStoriesService, IDisposable
{
    private const string CacheKey = "best-stories";

    private readonly IHackerNewsClient _client;
    private readonly IMemoryCache _cache;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<BestStoriesService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public BestStoriesService(
        IHackerNewsClient client,
        IMemoryCache cache,
        IOptions<HackerNewsOptions> options,
        ILogger<BestStoriesService> logger)
    {
        _client = client;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        if (count > _options.MaxStories)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"Count cannot exceed {_options.MaxStories}.");
        }

        var stories = await GetOrRefreshStoriesAsync(cancellationToken);
        return stories.Take(count).ToList();
    }

    private async Task<IReadOnlyList<StoryDto>> GetOrRefreshStoriesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<StoryDto>? cached) && cached is not null)
        {
            return cached;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock to avoid stampedes.
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            _logger.LogInformation("Refreshing best stories cache from Hacker News");
            var stories = await FetchAndMapBestStoriesAsync(cancellationToken);

            _cache.Set(
                CacheKey,
                stories,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CacheDuration
                });

            return stories;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<IReadOnlyList<StoryDto>> FetchAndMapBestStoriesAsync(CancellationToken cancellationToken)
    {
        var ids = await _client.GetBestStoryIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return Array.Empty<StoryDto>();
        }

        using var throttle = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);

        var tasks = ids.Select(async id =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                var item = await _client.GetItemAsync(id, cancellationToken);
                return StoryMapper.ToStoryDto(item);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to fetch Hacker News item {ItemId}", id);
                return null;
            }
            finally
            {
                throttle.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(story => story is not null)
            .Cast<StoryDto>()
            .OrderByDescending(story => story.Score)
            .ThenByDescending(story => story.Time)
            .ToList();
    }

    public void Dispose()
    {
        _refreshLock.Dispose();
    }
}
