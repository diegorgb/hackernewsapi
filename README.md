# Hacker News Best Stories API

ASP.NET Core RESTful API that returns the best *n* stories from the [Hacker News API](https://github.com/HackerNews/API), ordered by score descending. Responses are cached in memory so the service can handle high request volume without overloading Hacker News.

## Requirements

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later

## How to run

```bash
# Restore and build
dotnet restore
dotnet build

# Run the API
dotnet run --project src/HackerNewsApi

# Run unit tests
dotnet test
```

By default the API listens on:

- `https://localhost:7173`
- `http://localhost:5005`

Swagger UI (Development): `https://localhost:7173/swagger`

### Example request

```bash
curl "http://localhost:5005/api/beststories?n=5"
```

### Example response

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/beststories?n={n}` | Returns the best `n` stories by score (descending). `n` must be between **1** and **500**. |

## Configuration

Settings live under the `HackerNews` section in `src/HackerNewsApi/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | Hacker News Firebase API base URL |
| `CacheDuration` | `00:05:00` | How long the best-stories list is cached |
| `MaxConcurrentRequests` | `20` | Max parallel item fetches when refreshing the cache |
| `MaxStories` | `500` | Upper bound for `n` (matches HN `beststories` size) |

## Architecture

```
Client → BestStoriesController → BestStoriesService → IMemoryCache
                                      ↓ (cache miss)
                               IHackerNewsClient → HN Firebase API
```

1. `GET /api/beststories?n=...` validates `n` and calls `IBestStoriesService`.
2. On a cache hit, the service returns the top `n` stories from the cached list.
3. On a cache miss, a single refresh is performed (stampede-safe via `SemaphoreSlim`):
   - Fetch best story IDs from `/v0/beststories.json`
   - Fetch each item in parallel (throttled)
   - Map to the response DTO, sort by score descending, cache the full list

## Assumptions

1. **`.NET 6`** is an acceptable target (LTS at the time of writing / available on the machine used for development).
2. **`n` is a query parameter** (`?n=`). The brief did not mandate a route shape.
3. Stories are taken from HN **`beststories`**, then **re-sorted by `score` descending** as required. When scores tie, newer stories (`time`) come first.
4. Items that are **deleted**, **dead**, missing a **title**, or not of type **`story`** are skipped.
5. When a story has **no `url`** (e.g. Ask HN), `uri` is set to the HN discussion page: `https://news.ycombinator.com/item?id={id}`.
6. **`commentCount`** maps from HN’s `descendants` field; **`postedBy`** from `by`; **`time`** from Unix seconds to ISO-8601 UTC.
7. In-memory caching with a **5-minute TTL** is sufficient for a single-instance deployment and meets the “don’t overload HN” requirement.
8. Individual item fetch failures during a refresh are logged and skipped rather than failing the whole request.

## Enhancements / changes given more time

- **Distributed cache** (Redis) so multiple instances share one view of best stories.
- **Background refresh** (hosted service) to warm/refresh the cache proactively and keep request latency predictable.
- **Output caching / HTTP caching headers** (`Cache-Control`, ETag) for CDN / client-side reuse.
- **Resilience**: Polly retry/circuit-breaker around the HN client; health checks for readiness.
- **Observability**: OpenTelemetry metrics (cache hit rate, upstream latency, refresh duration).
- **Integration tests** with `WebApplicationFactory` and a stubbed `HttpMessageHandler`.
- **Pagination** or a richer filter API if consumers need more than a simple top-n list.
- **Rate limiting** on this API itself to protect downstream resources under abusive clients.
- Target **.NET 8** for long-term support and performance improvements.

## Project layout

```
HackerNewsApi.sln
src/HackerNewsApi/          # ASP.NET Core Web API
tests/HackerNewsApi.Tests/  # xUnit unit tests (mapper, client, service, controller)
```
