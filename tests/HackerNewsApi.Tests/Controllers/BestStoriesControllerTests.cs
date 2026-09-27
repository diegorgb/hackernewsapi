using HackerNewsApi.Configuration;
using HackerNewsApi.Controllers;
using HackerNewsApi.Models;
using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HackerNewsApi.Tests.Controllers;

public class BestStoriesControllerTests
{
    [Fact]
    public async Task GetBestStories_ReturnsOk_WithStories()
    {
        var stories = new List<StoryDto>
        {
            new()
            {
                Title = "Story",
                Uri = "https://example.com",
                PostedBy = "user",
                Time = DateTimeOffset.UtcNow,
                Score = 10,
                CommentCount = 2
            }
        };

        var service = new Mock<IBestStoriesService>();
        service.Setup(s => s.GetBestStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stories);

        var controller = CreateController(service.Object);

        var result = await controller.GetBestStories(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(stories, ok.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task GetBestStories_ReturnsBadRequest_WhenNIsOutOfRange(int n)
    {
        var service = new Mock<IBestStoriesService>(MockBehavior.Strict);
        var controller = CreateController(service.Object, maxStories: 500);

        var result = await controller.GetBestStories(n, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBestStories_ReturnsBadGateway_WhenServiceThrows()
    {
        var service = new Mock<IBestStoriesService>();
        service.Setup(s => s.GetBestStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upstream down"));

        var controller = CreateController(service.Object);

        var result = await controller.GetBestStories(5, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    private static BestStoriesController CreateController(
        IBestStoriesService service,
        int maxStories = 500)
    {
        var options = Options.Create(new HackerNewsOptions { MaxStories = maxStories });
        return new BestStoriesController(service, options, NullLogger<BestStoriesController>.Instance);
    }
}
