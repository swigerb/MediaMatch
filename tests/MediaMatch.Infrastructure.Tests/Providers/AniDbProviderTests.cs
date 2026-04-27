using System.Net;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class AniDbProviderTests
{
    private static AniDbConfiguration DefaultConfig => new()
    {
        BaseUrl = "http://api.anidb.test/httpapi",
        ClientName = "testclient",
        ClientVersion = 1,
        ProtocolVersion = 1,
        RateLimitIntervalMs = 0, // no delay in tests
        MaxRetries = 0,
        TimeoutSeconds = 5
    };

    private static (AniDbProvider provider, Mock<HttpMessageHandler> handler) CreateProvider(
        HttpStatusCode statusCode = HttpStatusCode.OK, string content = "<anime/>")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://api.anidb.test/") };
        var cache = new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        var provider = new AniDbProvider(httpClient, cache, DefaultConfig, NullLogger<AniDbProvider>.Instance);
        return (provider, handler);
    }

    [Fact]
    public void Name_ShouldBeAniDB()
    {
        var (provider, _) = CreateProvider();
        provider.Name.Should().Be("AniDB");
    }

    [Fact]
    public async Task SearchAnimeAsync_WithResults_ReturnsSearchResults()
    {
        var xml = @"<animetitles>
            <anime id=""1"">
                <title type=""main"">Cowboy Bebop</title>
                <title type=""official"">カウボーイビバップ</title>
            </anime>
            <anime id=""2"">
                <title type=""main"">Trigun</title>
            </anime>
        </animetitles>";

        var (provider, _) = CreateProvider(content: xml);
        var results = await provider.SearchAnimeAsync("Cowboy Bebop");

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Cowboy Bebop");
        results[0].Id.Should().Be(1);
        results[0].AliasNames.Should().Contain("カウボーイビバップ");
    }

    [Fact]
    public async Task SearchAnimeAsync_EmptyResponse_ReturnsEmpty()
    {
        var (provider, _) = CreateProvider(content: "<anime/>");
        var results = await provider.SearchAnimeAsync("Nothing");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAnimeAsync_ErrorElement_ReturnsEmpty()
    {
        var xml = "<anime><error>Banned</error></anime>";
        var (provider, _) = CreateProvider(content: xml);
        var results = await provider.SearchAnimeAsync("Test");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnimeEpisodesAsync_ParsesRegularAndSpecialEpisodes()
    {
        var xml = @"<anime>
            <title type=""main"">Cowboy Bebop</title>
            <episode>
                <epno type=""1"">1</epno>
                <title xml:lang=""en"">Asteroid Blues</title>
                <airdate>1998-10-24</airdate>
            </episode>
            <episode>
                <epno type=""1"">2</epno>
                <title xml:lang=""en"">Stray Dog Strut</title>
            </episode>
            <episode>
                <epno type=""2"">S1</epno>
                <title>Special 1</title>
            </episode>
            <episode>
                <epno type=""3"">C1</epno>
                <title>Credit</title>
            </episode>
        </anime>";

        var (provider, _) = CreateProvider(content: xml);
        var episodes = await provider.GetAnimeEpisodesAsync(1);

        // Should include regular + special, skip credits (type=3)
        episodes.Should().HaveCount(3);
        episodes[0].Season.Should().Be(0); // Special first (sorted by season)
        episodes[1].Season.Should().Be(1);
        episodes[1].EpisodeNumber.Should().Be(1);
        episodes[1].Title.Should().Be("Asteroid Blues");
        episodes[1].AbsoluteNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAnimeInfoAsync_ParsesSeriesInfo()
    {
        var xml = @"<anime>
            <title type=""main"">Cowboy Bebop</title>
            <title type=""official"">カウボーイビバップ</title>
            <description>See you space cowboy</description>
            <startdate>1998-04-03</startdate>
            <type>TV Series</type>
            <ratings><permanent>8.5</permanent></ratings>
            <tag><name>action</name></tag>
            <tag><name>sci-fi</name></tag>
        </anime>";

        var (provider, _) = CreateProvider(content: xml);
        var info = await provider.GetAnimeInfoAsync(42);

        info.Name.Should().Be("Cowboy Bebop");
        info.Overview.Should().Be("See you space cowboy");
        info.Status.Should().Be("TV Series");
        info.Rating.Should().Be(8.5);
        info.Genres.Should().Contain("action");
        info.AliasNames.Should().Contain("カウボーイビバップ");
    }

    [Fact]
    public async Task GetAnimeInfoAsync_NullResponse_Throws()
    {
        var (provider, _) = CreateProvider(content: "");
        await provider.Invoking(p => p.GetAnimeInfoAsync(999))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchAsync_DelegatesTo_SearchAnimeAsync()
    {
        var xml = @"<animetitles><anime id=""10""><title type=""main"">Test</title></anime></animetitles>";
        var (provider, _) = CreateProvider(content: xml);
        var results = await provider.SearchAsync("Test");
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetEpisodesAsync_DelegatesTo_GetAnimeEpisodesAsync()
    {
        var xml = @"<anime>
            <title type=""main"">Test</title>
            <episode><epno type=""1"">1</epno><title>Ep1</title></episode>
        </anime>";
        var (provider, _) = CreateProvider(content: xml);
        var searchResult = new SearchResult("Test", 1);
        var episodes = await provider.GetEpisodesAsync(searchResult);
        episodes.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAnimeAsync_HttpFailure_WithMaxRetriesZero_Throws()
    {
        var (provider, _) = CreateProvider(HttpStatusCode.InternalServerError, "error");
        await provider.Invoking(p => p.SearchAnimeAsync("Test"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SearchAnimeAsync_CachesResults()
    {
        var xml = @"<animetitles><anime id=""1""><title type=""main"">Cached</title></anime></animetitles>";
        var (provider, handler) = CreateProvider(content: xml);

        var r1 = await provider.SearchAnimeAsync("Cached");
        var r2 = await provider.SearchAnimeAsync("Cached");

        r1.Should().BeSameAs(r2);
        handler.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
