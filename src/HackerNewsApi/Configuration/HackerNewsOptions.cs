using System.ComponentModel.DataAnnotations;

namespace HackerNewsApi.Configuration;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    /// <summary>
    /// Base URL of the Hacker News Firebase API (must end with '/').
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";

    /// <summary>
    /// How long best-story data is cached before being refreshed from Hacker News.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum concurrent requests when fetching individual story details.
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentRequests { get; set; } = 20;

    /// <summary>
    /// Maximum number of best stories that can be requested (HN returns up to 500).
    /// </summary>
    [Range(1, 500)]
    public int MaxStories { get; set; } = 500;
}
