using System.Net;
using System.Text;
using HackerNewsApi.Clients;
using HackerNewsApi.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Tests.Clients;

public class HackerNewsClientTests
{
    [Fact]
    public async Task GetBestStoryIdsAsync_ReturnsIds_WhenResponseIsValid()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[1,2,3]", Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);

        var ids = await client.GetBestStoryIdsAsync();

        Assert.Equal(new[] { 1, 2, 3 }, ids);
        Assert.Contains("beststories.json", handler.LastRequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ReturnsEmpty_WhenBodyIsNullArrayMissing()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);

        var ids = await client.GetBestStoryIdsAsync();

        Assert.Empty(ids);
    }

    [Fact]
    public async Task GetItemAsync_ReturnsItem_WhenResponseIsValid()
    {
        const string json = @"{
  ""id"": 21233041,
  ""by"": ""ismaildonmez"",
  ""score"": 1716,
  ""time"": 1570887781,
  ""title"": ""A uBlock Origin update was rejected from the Chrome Web Store"",
  ""type"": ""story"",
  ""url"": ""https://github.com/uBlockOrigin/uBlock-issues/issues/745"",
  ""descendants"": 572
}";

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);

        var item = await client.GetItemAsync(21233041);

        Assert.NotNull(item);
        Assert.Equal(21233041, item!.Id);
        Assert.Equal("ismaildonmez", item.By);
        Assert.Equal(1716, item.Score);
        Assert.Equal(572, item.Descendants);
        Assert.Contains("item/21233041.json", handler.LastRequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetItemAsync_ReturnsNull_WhenNotFound()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = CreateClient(handler);

        var item = await client.GetItemAsync(999);

        Assert.Null(item);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_Throws_WhenUpstreamReturnsError()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetBestStoryIdsAsync());
    }

    private static HackerNewsClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new HackerNewsOptions
        {
            BaseUrl = "https://hacker-news.firebaseio.com/v0/"
        });

        return new HackerNewsClient(httpClient, options, NullLogger<HackerNewsClient>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(_responder(request));
        }
    }
}
