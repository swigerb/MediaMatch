using System.Net;
using System.Text;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class TmdbEpisodeProviderTests
{
    private static readonly ApiConfiguration DefaultConfig = new()
    {
        TmdbApiKey = "test-key",
        TmdbBaseUrl = "https://api.tmdb.org/3",
        TmdbImageBaseUrl = "https://image.tmdb.org/t/p/original",
        Language = "en-US"
    };

    private static TmdbEpisodeProvider CreateProvider(HttpMessageHandler handler, MetadataCache? cache = null)
    {
        var httpClient = new HttpClient(handler);
        var mmHttp = new MediaMatchHttpClient(
            httpClient,
            NullLogger<MediaMatchHttpClient>.Instance,
            maxRetries: 0);
        cache ??= new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        return new TmdbEpisodeProvider(
            mmHttp,
            cache,
            DefaultConfig,
            NullLogger<TmdbEpisodeProvider>.Instance);
    }

    private static HttpMessageHandler CreateMockHandler(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        return handler.Object;
    }

    private static HttpMessageHandler CreateUrlRoutingHandler(Dictionary<string, string> urlResponses)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                var url = req.RequestUri!.ToString();
                foreach (var (pattern, json) in urlResponses)
                {
                    if (url.Contains(pattern))
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json")
                        };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
        return handler.Object;
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsResults()
    {
        const string json = """{"results":[{"id":1396,"name":"Breaking Bad","original_name":"Breaking Bad"}]}""";
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, json));

        var results = await provider.SearchAsync("Breaking Bad");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Breaking Bad");
        results[0].Id.Should().Be(1396);
    }

    [Fact]
    public async Task SearchAsync_NullResponse_ReturnsEmpty()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));

        var results = await provider.SearchAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEpisodesAsync_ValidSeries_ReturnsEpisodes()
    {
        const string detailJson = """
            {
                "id":1396,"name":"Breaking Bad","overview":"A chemistry teacher...",
                "status":"Ended","poster_path":"/bb.jpg","vote_average":8.9,
                "first_air_date":"2008-01-20","original_language":"en",
                "episode_run_time":[47],"origin_country":["US"],
                "genres":[{"name":"Drama"}],"networks":[{"name":"AMC"}],
                "seasons":[{"season_number":1}],"external_ids":{"imdb_id":"tt0903747"}
            }
            """;
        const string seasonJson = """
            {"episodes":[{"episode_number":1,"name":"Pilot","air_date":"2008-01-20"},{"episode_number":2,"name":"Cat's in the Bag...","air_date":"2008-01-27"}]}
            """;

        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/tv/1396/season/1", seasonJson },
            { "/tv/1396?", detailJson }
        });
        var provider = CreateProvider(handler);
        var series = new SearchResult("Breaking Bad", 1396);

        var episodes = await provider.GetEpisodesAsync(series);

        episodes.Should().HaveCount(2);
        episodes[0].SeriesName.Should().Be("Breaking Bad");
        episodes[0].Season.Should().Be(1);
        episodes[0].EpisodeNumber.Should().Be(1);
        episodes[0].Title.Should().Be("Pilot");
        episodes[1].Title.Should().Be("Cat's in the Bag...");
    }

    [Fact]
    public async Task GetEpisodesAsync_NullSeasons_ReturnsEmpty()
    {
        const string json = """{"id":1396,"name":"Breaking Bad","seasons":null}""";
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, json));
        var series = new SearchResult("Breaking Bad", 1396);

        var episodes = await provider.GetEpisodesAsync(series);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeriesInfoAsync_ValidSeries_ReturnsInfo()
    {
        const string json = """
            {
                "id":1396,"name":"Breaking Bad","overview":"A chemistry teacher...",
                "status":"Ended","poster_path":"/bb.jpg","vote_average":8.9,
                "first_air_date":"2008-01-20","original_language":"en",
                "episode_run_time":[47],"origin_country":["US"],
                "genres":[{"name":"Drama"}],"networks":[{"name":"AMC"}],
                "seasons":[{"season_number":1}],
                "external_ids":{"imdb_id":"tt0903747"}
            }
            """;
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, json));
        var series = new SearchResult("Breaking Bad", 1396);

        var info = await provider.GetSeriesInfoAsync(series);

        info.Name.Should().Be("Breaking Bad");
        info.Overview.Should().Be("A chemistry teacher...");
        info.Network.Should().Be("AMC");
        info.Status.Should().Be("Ended");
        info.Rating.Should().Be(8.9);
        info.Runtime.Should().Be(47);
        info.Genres.Should().ContainSingle().Which.Should().Be("Drama");
        info.PosterUrl.Should().Contain("/bb.jpg");
        info.ImdbId.Should().Be("tt0903747");
        info.TmdbId.Should().Be(1396);
        info.Language.Should().Be("en");
    }

    [Fact]
    public async Task GetSeriesInfoAsync_NullResponse_ThrowsInvalidOperation()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));
        var series = new SearchResult("Breaking Bad", 1396);

        var act = () => provider.GetSeriesInfoAsync(series);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TmdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") })
            .Verifiable();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TmdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TmdbEpisodeProvider>.Instance);

        var results = await provider.SearchAsync("Breaking Bad");

        results.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetEpisodesAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TmdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TmdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TmdbEpisodeProvider>.Instance);

        var results = await provider.GetEpisodesAsync(new SearchResult("Test", 1));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeriesInfoAsync_NoApiKey_ReturnsMinimalInfo()
    {
        var config = new ApiConfiguration { TmdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TmdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TmdbEpisodeProvider>.Instance);

        var info = await provider.GetSeriesInfoAsync(new SearchResult("Test Show", 42));

        info.Name.Should().Be("Test Show");
        info.Id.Should().Be("42");
    }
}
