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

public sealed class TvdbEpisodeProviderTests
{
    private const string LoginResponse = """{"status":"success","data":{"token":"fake-token"}}""";

    private static readonly ApiConfiguration DefaultConfig = new()
    {
        TvdbApiKey = "tvdb-test-key",
        TvdbBaseUrl = "https://api4.thetvdb.com/v4",
        TmdbApiKey = "tmdb-test-key",
        TmdbBaseUrl = "https://api.tmdb.org/3",
        TmdbImageBaseUrl = "https://image.tmdb.org/t/p/original",
        Language = "en-US"
    };

    private static TvdbEpisodeProvider CreateProvider(HttpMessageHandler handler, MetadataCache? cache = null)
    {
        var httpClient = new HttpClient(handler);
        var mmHttp = new MediaMatchHttpClient(
            httpClient,
            NullLogger<MediaMatchHttpClient>.Instance,
            maxRetries: 0);
        cache ??= new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        return new TvdbEpisodeProvider(
            mmHttp,
            cache,
            DefaultConfig,
            NullLogger<TvdbEpisodeProvider>.Instance);
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

    [Fact]
    public async Task SearchAsync_AuthenticatesFirst_ThenSearches()
    {
        const string searchJson = """{"status":"success","data":[{"tvdbId":"1","name":"Breaking Bad","aliases":["BB"]}]}""";
        var requestLog = new List<string>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                var url = req.RequestUri!.ToString();
                requestLog.Add(url);
                string json = url.Contains("/login") ? LoginResponse : searchJson;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var provider = CreateProvider(handler.Object);

        var results = await provider.SearchAsync("Breaking Bad");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Breaking Bad");
        results[0].Id.Should().Be(1);
        results[0].AliasNames.Should().Contain("BB");

        requestLog.Should().HaveCount(2);
        requestLog[0].Should().Contain("/login");
        requestLog[1].Should().Contain("/search");
    }

    [Fact]
    public async Task SearchAsync_NullResponse_ReturnsEmpty()
    {
        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/login", LoginResponse },
            { "/search", """{"status":"success","data":null}""" }
        });
        var provider = CreateProvider(handler);

        var results = await provider.SearchAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEpisodesAsync_ValidSeries_ReturnsEpisodes()
    {
        const string episodesJson = """{"status":"success","data":{"episodes":[{"seasonNumber":1,"number":1,"name":"Pilot","aired":"2008-01-20","absoluteNumber":1}]}}""";
        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/login", LoginResponse },
            { "/series/1/episodes/", episodesJson }
        });
        var provider = CreateProvider(handler);
        var series = new SearchResult("Breaking Bad", 1);

        var episodes = await provider.GetEpisodesAsync(series);

        episodes.Should().ContainSingle();
        episodes[0].SeriesName.Should().Be("Breaking Bad");
        episodes[0].Season.Should().Be(1);
        episodes[0].EpisodeNumber.Should().Be(1);
        episodes[0].Title.Should().Be("Pilot");
        episodes[0].AbsoluteNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetEpisodesAsync_EmptyEpisodes_ReturnsEmpty()
    {
        const string emptyJson = """{"status":"success","data":{"episodes":[]}}""";
        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/login", LoginResponse },
            { "/series/1/episodes/", emptyJson }
        });
        var provider = CreateProvider(handler);
        var series = new SearchResult("Breaking Bad", 1);

        var episodes = await provider.GetEpisodesAsync(series);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeriesInfoAsync_ValidSeries_ReturnsInfo()
    {
        const string extendedJson = """
            {
                "status":"success",
                "data":{
                    "name":"Breaking Bad","overview":"A teacher...",
                    "image":"https://img.tvdb.com/bb.jpg","firstAired":"2008-01-20",
                    "originalLanguage":"en","score":9.0,"averageRuntime":47,
                    "originalNetwork":{"name":"AMC"},
                    "status":{"name":"Ended"},
                    "genres":[{"name":"Drama"}],
                    "aliases":[{"name":"BB"}]
                }
            }
            """;
        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/login", LoginResponse },
            { "/series/1/extended", extendedJson }
        });
        var provider = CreateProvider(handler);
        var series = new SearchResult("Breaking Bad", 1);

        var info = await provider.GetSeriesInfoAsync(series);

        info.Name.Should().Be("Breaking Bad");
        info.Overview.Should().Be("A teacher...");
        info.Network.Should().Be("AMC");
        info.Status.Should().Be("Ended");
        info.Rating.Should().Be(9.0);
        info.Runtime.Should().Be(47);
        info.Genres.Should().ContainSingle().Which.Should().Be("Drama");
        info.PosterUrl.Should().Be("https://img.tvdb.com/bb.jpg");
        info.Language.Should().Be("en");
        info.AliasNames.Should().Contain("BB");
    }

    [Fact]
    public async Task GetSeriesInfoAsync_NullData_ThrowsInvalidOperation()
    {
        var handler = CreateUrlRoutingHandler(new Dictionary<string, string>
        {
            { "/login", LoginResponse },
            { "/series/1/extended", """{"status":"success","data":null}""" }
        });
        var provider = CreateProvider(handler);
        var series = new SearchResult("Breaking Bad", 1);

        var act = () => provider.GetSeriesInfoAsync(series);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TvdbApiKey = "", TmdbApiKey = "x" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") })
            .Verifiable();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TvdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TvdbEpisodeProvider>.Instance);

        var results = await provider.SearchAsync("Breaking Bad");

        results.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetEpisodesAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TvdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TvdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TvdbEpisodeProvider>.Instance);

        var results = await provider.GetEpisodesAsync(new SearchResult("Test", 1));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeriesInfoAsync_NoApiKey_ReturnsMinimalInfo()
    {
        var config = new ApiConfiguration { TvdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TvdbEpisodeProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TvdbEpisodeProvider>.Instance);

        var info = await provider.GetSeriesInfoAsync(new SearchResult("Test Show", 42));

        info.Name.Should().Be("Test Show");
        info.Id.Should().Be("42");
    }
}
