using HackerNewsApi.Configuration;
using HackerNewsApi.Models;
using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class BestStoriesController : ControllerBase
{
    private readonly IBestStoriesService _bestStoriesService;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<BestStoriesController> _logger;

    public BestStoriesController(
        IBestStoriesService bestStoriesService,
        IOptions<HackerNewsOptions> options,
        ILogger<BestStoriesController> logger)
    {
        _bestStoriesService = bestStoriesService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the best <paramref name="n"/> Hacker News stories ordered by score descending.
    /// </summary>
    /// <param name="n">Number of stories to return (1 to MaxStories).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<StoryDto>>> GetBestStories(
        [FromQuery] int n,
        CancellationToken cancellationToken)
    {
        if (n < 1 || n > _options.MaxStories)
        {
            return BadRequest(new
            {
                error = $"Parameter 'n' must be between 1 and {_options.MaxStories}."
            });
        }

        try
        {
            var stories = await _bestStoriesService.GetBestStoriesAsync(n, cancellationToken);
            return Ok(stories);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve best stories for n={Count}", n);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Failed to retrieve stories from Hacker News."
            });
        }
    }
}
