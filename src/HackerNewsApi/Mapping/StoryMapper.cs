using HackerNewsApi.Models;

namespace HackerNewsApi.Mapping;

public static class StoryMapper
{
    /// <summary>
    /// Maps a Hacker News item to the API story DTO.
    /// Returns null when the item is not a usable story (deleted, dead, missing title, etc.).
    /// </summary>
    public static StoryDto? ToStoryDto(HackerNewsItem? item)
    {
        if (item is null)
        {
            return null;
        }

        if (item.Deleted == true || item.Dead == true)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            return null;
        }

        // Jobs and polls can appear in some HN lists; beststories is story-oriented.
        // Accept "story" and items without type (defensive); skip clear non-stories.
        if (!string.IsNullOrEmpty(item.Type)
            && !string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var uri = !string.IsNullOrWhiteSpace(item.Url)
            ? item.Url!
            : $"https://news.ycombinator.com/item?id={item.Id}";

        return new StoryDto
        {
            Title = item.Title!,
            Uri = uri,
            PostedBy = item.By ?? string.Empty,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score = item.Score ?? 0,
            CommentCount = item.Descendants ?? 0
        };
    }
}
