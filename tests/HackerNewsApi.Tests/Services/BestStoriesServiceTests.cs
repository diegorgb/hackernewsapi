using HackerNewsApi.Clients;
using HackerNewsApi.Configuration;
using HackerNewsApi.Models;
using HackerNewsApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HackerNewsApi.Tests.Services;

public class BestStoriesServiceTests
{
    [Fact]
    public async Task GetBestStoriesAsync_ReturnsTopNSortedByScoreDescending()
    {
        var client = new Mock<IHackerNewsClient>();
        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2, 3 });

        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(1, score: 10));
        client.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(2, score: 50));
        client.Setup(c => c.GetItemAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(3, score: 30));

        var service = CreateService(client.Object);

        var result = await service.GetBestStoriesAsync(2);

        Assert.Equal(2, result.Count);
        Assert.Equal(50, result[0].Score);
        Assert.Equal(30, result[1].Score);
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCache_OnSubsequentCalls()
    {
        var client = new Mock<IHackerNewsClient>();
        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1 });
        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(1, score: 100));

        var service = CreateService(client.Object);

        _ = await service.GetBestStoriesAsync(1);
        _ = await service.GetBestStoriesAsync(1);

        client.Verify(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBestStoriesAsync_SkipsNullOrInvalidItems()
    {
        var client = new Mock<IHackerNewsClient>();
        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2, 3 });

        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HackerNewsItem?)null);
        client.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HackerNewsItem { Id = 2, Title = "Deleted", Deleted = true, Time = 1 });
        client.Setup(c => c.GetItemAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(3, score: 42));

        var service = CreateService(client.Object);

        var result = await service.GetBestStoriesAsync(3);

        Assert.Single(result);
        Assert.Equal(42, result[0].Score);
    }

    [Fact]
    public async Task GetBestStoriesAsync_Continues_WhenIndividualItemFetchFails()
    {
        var client = new Mock<IHackerNewsClient>();
        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2 });

        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));
        client.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(2, score: 7));

        var service = CreateService(client.Object);

        var result = await service.GetBestStoriesAsync(2);

        Assert.Single(result);
        Assert.Equal(7, result[0].Score);
    }

    [Fact]
    public async Task GetBestStoriesAsync_Throws_WhenCountIsLessThanOne()
    {
        var service = CreateService(Mock.Of<IHackerNewsClient>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetBestStoriesAsync(0));
    }

    [Fact]
    public async Task GetBestStoriesAsync_Throws_WhenCountExceedsMax()
    {
        var service = CreateService(Mock.Of<IHackerNewsClient>(), maxStories: 10);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetBestStoriesAsync(11));
    }

    [Fact]
    public async Task GetBestStoriesAsync_OnlyOneUpstreamRefresh_UnderConcurrentLoad()
    {
        var fetchStarted = new TaskCompletionSource();
        var allowFetchToFinish = new TaskCompletionSource();
        var callCount = 0;

        var client = new Mock<IHackerNewsClient>();
        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref callCount);
                fetchStarted.TrySetResult();
                await allowFetchToFinish.Task;
                return (IReadOnlyList<int>)new[] { 1 };
            });
        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(1, score: 1));

        var service = CreateService(client.Object);

        var first = service.GetBestStoriesAsync(1);
        await fetchStarted.Task;

        var second = service.GetBestStoriesAsync(1);
        allowFetchToFinish.SetResult();

        await Task.WhenAll(first, second);

        Assert.Equal(1, callCount);
    }

    private static BestStoriesService CreateService(
        IHackerNewsClient client,
        int maxStories = 500,
        TimeSpan? cacheDuration = null)
    {
        var options = Options.Create(new HackerNewsOptions
        {
            MaxStories = maxStories,
            MaxConcurrentRequests = 5,
            CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5)
        });

        return new BestStoriesService(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            options,
            NullLogger<BestStoriesService>.Instance);
    }

    private static HackerNewsItem CreateStory(int id, int score) => new()
    {
        Id = id,
        Type = "story",
        Title = $"Story {id}",
        Url = $"https://example.com/{id}",
        By = "author",
        Time = 1_000_000 + id,
        Score = score,
        Descendants = id
    };
}
