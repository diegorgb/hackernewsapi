using HackerNewsApi.Models;

namespace HackerNewsApi.Services;

public interface IBestStoriesService
{
    /// <summary>
    /// Returns the best <paramref name="count"/> stories ordered by score descending.
    /// </summary>
    Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
}
