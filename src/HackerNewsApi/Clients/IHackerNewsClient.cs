using HackerNewsApi.Models;

namespace HackerNewsApi.Clients;

public interface IHackerNewsClient
{
    /// <summary>
    /// Returns the IDs of the current best stories from Hacker News (up to 500).
    /// </summary>
    Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the details for a single item, or null if the item does not exist / was deleted.
    /// </summary>
    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken = default);
}
