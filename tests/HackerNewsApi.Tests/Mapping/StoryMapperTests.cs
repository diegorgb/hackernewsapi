using HackerNewsApi.Mapping;
using HackerNewsApi.Models;

namespace HackerNewsApi.Tests.Mapping;

public class StoryMapperTests
{
    [Fact]
    public void ToStoryDto_MapsAllFields_WhenItemIsValidStory()
    {
        var item = new HackerNewsItem
        {
            Id = 21233041,
            Type = "story",
            Title = "A uBlock Origin update was rejected from the Chrome Web Store",
            Url = "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            By = "ismaildonmez",
            Time = 1570887781,
            Score = 1716,
            Descendants = 572
        };

        var result = StoryMapper.ToStoryDto(item);

        Assert.NotNull(result);
        Assert.Equal(item.Title, result!.Title);
        Assert.Equal(item.Url, result.Uri);
        Assert.Equal(item.By, result.PostedBy);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1570887781), result.Time);
        Assert.Equal(1716, result.Score);
        Assert.Equal(572, result.CommentCount);
    }

    [Fact]
    public void ToStoryDto_UsesHnDiscussionUrl_WhenStoryHasNoUrl()
    {
        var item = new HackerNewsItem
        {
            Id = 121003,
            Type = "story",
            Title = "Ask HN: The Arc Effect",
            Url = null,
            By = "tel",
            Time = 1203647620,
            Score = 25,
            Descendants = 16
        };

        var result = StoryMapper.ToStoryDto(item);

        Assert.NotNull(result);
        Assert.Equal("https://news.ycombinator.com/item?id=121003", result!.Uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToStoryDto_ReturnsNull_WhenTitleMissing(string? title)
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Type = "story",
            Title = title,
            By = "user",
            Time = 1,
            Score = 1
        };

        Assert.Null(StoryMapper.ToStoryDto(item));
    }

    [Fact]
    public void ToStoryDto_ReturnsNull_WhenItemIsNull()
    {
        Assert.Null(StoryMapper.ToStoryDto(null));
    }

    [Fact]
    public void ToStoryDto_ReturnsNull_WhenItemIsDeleted()
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Title = "Gone",
            Deleted = true,
            Time = 1
        };

        Assert.Null(StoryMapper.ToStoryDto(item));
    }

    [Fact]
    public void ToStoryDto_ReturnsNull_WhenItemIsDead()
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Title = "Dead story",
            Dead = true,
            Time = 1
        };

        Assert.Null(StoryMapper.ToStoryDto(item));
    }

    [Theory]
    [InlineData("comment")]
    [InlineData("job")]
    [InlineData("poll")]
    [InlineData("pollopt")]
    public void ToStoryDto_ReturnsNull_WhenTypeIsNotStory(string type)
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Type = type,
            Title = "Not a story",
            Time = 1,
            Score = 10
        };

        Assert.Null(StoryMapper.ToStoryDto(item));
    }

    [Fact]
    public void ToStoryDto_DefaultsScoreAndComments_WhenMissing()
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Type = "story",
            Title = "Story",
            By = "author",
            Time = 100,
            Score = null,
            Descendants = null
        };

        var result = StoryMapper.ToStoryDto(item);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Score);
        Assert.Equal(0, result.CommentCount);
    }

    [Fact]
    public void ToStoryDto_UsesEmptyPostedBy_WhenAuthorMissing()
    {
        var item = new HackerNewsItem
        {
            Id = 1,
            Type = "story",
            Title = "Story",
            By = null,
            Time = 100,
            Score = 5
        };

        var result = StoryMapper.ToStoryDto(item);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.PostedBy);
    }
}
