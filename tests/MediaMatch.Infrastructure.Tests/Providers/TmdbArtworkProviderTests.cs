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

public sealed class TmdbArtworkProviderTests
{
    private static readonly ApiConfiguration DefaultConfig = new()
    {
        TmdbApiKey = "test-key",
        TmdbBaseUrl = "https://api.tmdb.org/3",
        TmdbImageBaseUrl = "https://image.tmdb.org/t/p/original",
        Language = "en-US"
    };

    private static readonly string ImagesJson = """
        {
            "posters":[{"filePath":"/poster.jpg","iso639_1":"en","voteAverage":5.3,"width":500,"height":750}],
            "backdrops":[{"filePath":"/backdrop.jpg","iso639_1":null,"voteAverage":5.0,"width":1920,"height":1080}],
            "logos":[{"filePath":"/logo.png","iso639_1":"en","voteAverage":4.0,"width":400,"height":200}]
        }
        """;

    private static TmdbArtworkProvider CreateProvider(HttpMessageHandler handler, MetadataCache? cache = null)
    {
        var httpClient = new HttpClient(handler);
        var mmHttp = new MediaMatchHttpClient(
            httpClient,
            NullLogger<MediaMatchHttpClient>.Instance,
            maxRetries: 0);
        cache ??= new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        return new TmdbArtworkProvider(
            mmHttp,
            cache,
            DefaultConfig,
            NullLogger<TmdbArtworkProvider>.Instance);
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
    public async Task GetArtworkAsync_ReturnsAllTypes()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, ImagesJson));

        var artwork = await provider.GetArtworkAsync(1396);

        artwork.Should().HaveCount(3);
        artwork.Should().Contain(a => a.Type == ArtworkType.Poster);
        artwork.Should().Contain(a => a.Type == ArtworkType.Fanart);
        artwork.Should().Contain(a => a.Type == ArtworkType.Clearlogo);

        var poster = artwork.First(a => a.Type == ArtworkType.Poster);
        poster.Url.Should().Contain("/poster.jpg");
        poster.Language.Should().Be("en");
        poster.Rating.Should().Be(5.3);
        poster.Width.Should().Be(500);
        poster.Height.Should().Be(750);

        var backdrop = artwork.First(a => a.Type == ArtworkType.Fanart);
        backdrop.Url.Should().Contain("/backdrop.jpg");
        backdrop.Width.Should().Be(1920);
        backdrop.Height.Should().Be(1080);
    }

    [Fact]
    public async Task GetArtworkAsync_WithTypeFilter_FiltersResults()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, ImagesJson));

        var artwork = await provider.GetArtworkAsync(1396, ArtworkType.Poster);

        artwork.Should().ContainSingle();
        artwork[0].Type.Should().Be(ArtworkType.Poster);
        artwork[0].Url.Should().Contain("/poster.jpg");
    }

    [Fact]
    public async Task GetArtworkAsync_NullResponse_ReturnsEmpty()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));

        var artwork = await provider.GetArtworkAsync(1396);

        artwork.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovieArtworkAsync_ReturnsArtwork()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, ImagesJson));

        var artwork = await provider.GetMovieArtworkAsync(550);

        artwork.Should().HaveCount(3);
        artwork.Should().Contain(a => a.Type == ArtworkType.Poster);
        artwork.Should().Contain(a => a.Type == ArtworkType.Fanart);
        artwork.Should().Contain(a => a.Type == ArtworkType.Clearlogo);
    }

    [Fact]
    public async Task GetMovieArtworkAsync_NullResponse_ReturnsEmpty()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));

        var artwork = await provider.GetMovieArtworkAsync(550);

        artwork.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArtworkAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TmdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") })
            .Verifiable();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TmdbArtworkProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TmdbArtworkProvider>.Instance);

        var artwork = await provider.GetArtworkAsync(123);

        artwork.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetMovieArtworkAsync_NoApiKey_ReturnsEmpty()
    {
        var config = new ApiConfiguration { TmdbApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") })
            .Verifiable();
        var httpClient = new HttpClient(handler.Object);
        var mmHttp = new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries: 0);
        var provider = new TmdbArtworkProvider(mmHttp, new MetadataCache(new MemoryCache(new MemoryCacheOptions())), config, NullLogger<TmdbArtworkProvider>.Instance);

        var artwork = await provider.GetMovieArtworkAsync(550);

        artwork.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
