using System.Net;
using System.Text.Json;
using HackerNewsApi.Configuration;
using HackerNewsApi.Models;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Clients;

public sealed class HackerNewsClient : IHackerNewsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsClient> _logger;

    public HackerNewsClient(
        HttpClient httpClient,
        IOptions<HackerNewsOptions> options,
        ILogger<HackerNewsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = options.Value.BaseUrl;
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("beststories.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var ids = await JsonSerializer.DeserializeAsync<List<int>>(stream, JsonOptions, cancellationToken);

        return ids ?? (IReadOnlyList<int>)Array.Empty<int>();
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"item/{id}.json", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var item = await JsonSerializer.DeserializeAsync<HackerNewsItem>(stream, JsonOptions, cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Hacker News returned a null body for item {ItemId}", id);
        }

        return item;
    }
}
