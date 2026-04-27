using System.Net;
using System.Text;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public class TmdbMovieProviderTests
{
    private static readonly ApiConfiguration DefaultConfig = new()
    {
        TmdbApiKey = "test-key",
        TmdbBaseUrl = "https://api.tmdb.org/3",
        TmdbImageBaseUrl = "https://image.tmdb.org/t/p/original",
        Language = "en-US"
    };

    private static TmdbMovieProvider CreateProvider(HttpMessageHandler handler, MetadataCache? cache = null)
    {
        var httpClient = new HttpClient(handler);
        var mmHttp = new MediaMatchHttpClient(
            httpClient,
            NullLogger<MediaMatchHttpClient>.Instance,
            maxRetries: 0);
        cache ??= new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        return new TmdbMovieProvider(
            mmHttp,
            cache,
            DefaultConfig,
            NullLogger<TmdbMovieProvider>.Instance);
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

    private static Mock<HttpMessageHandler> CreateVerifiableMockHandler(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            })
            .Verifiable();
        return handler;
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsMovies()
    {
        const string json = """
            {"results":[{"id":550,"title":"Fight Club","originalTitle":"Fight Club","releaseDate":"1999-10-15","originalLanguage":"en"}]}
            """;
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, json));

        var results = await provider.SearchAsync("Fight Club");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Fight Club");
        results[0].Year.Should().Be(1999);
        results[0].TmdbId.Should().Be(550);
        results[0].Language.Should().Be("en");
    }

    [Fact]
    public async Task SearchAsync_WithYear_IncludesYearInUrl()
    {
        const string json = """{"results":[{"id":550,"title":"Fight Club","originalTitle":"Fight Club","releaseDate":"1999-10-15","originalLanguage":"en"}]}""";
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                req.RequestUri!.ToString().Should().Contain("&year=1999");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var provider = CreateProvider(handler.Object);

        var results = await provider.SearchAsync("Fight Club", year: 1999);

        results.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchAsync_NullResponse_ReturnsEmptyList()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));

        var results = await provider.SearchAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmptyList()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, """{"results":[]}"""));

        var results = await provider.SearchAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovieInfoAsync_ValidMovie_ReturnsMovieInfo()
    {
        const string json = """
            {
                "id":550,"title":"Fight Club","originalTitle":"Fight Club",
                "overview":"An insomniac...","tagline":"Mischief...",
                "releaseDate":"1999-10-15","posterPath":"/poster.jpg",
                "voteAverage":8.4,"runtime":139,"imdbId":"tt0137523",
                "originalLanguage":"en","revenue":100853753,"budget":63000000,
                "genres":[{"name":"Drama"}],
                "credits":{
                    "cast":[{"id":819,"name":"Edward Norton","character":"The Narrator","profilePath":"/profile.jpg","order":0}],
                    "crew":[{"id":7467,"name":"David Fincher","department":"Directing","job":"Director","profilePath":null}]
                },
                "releaseDates":{"results":[{"iso3166_1":"US","releaseDates":[{"certification":"R"}]}]},
                "belongsToCollection":null
            }
            """;
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, json));

        var movie = new Movie("Fight Club", 1999, TmdbId: 550);
        var info = await provider.GetMovieInfoAsync(movie);

        info.Name.Should().Be("Fight Club");
        info.Year.Should().Be(1999);
        info.TmdbId.Should().Be(550);
        info.ImdbId.Should().Be("tt0137523");
        info.Overview.Should().Be("An insomniac...");
        info.Tagline.Should().Be("Mischief...");
        info.PosterUrl.Should().Contain("/poster.jpg");
        info.Rating.Should().Be(8.4);
        info.Runtime.Should().Be(139);
        info.Certification.Should().Be("R");
        info.Genres.Should().ContainSingle().Which.Should().Be("Drama");
        info.Cast.Should().ContainSingle().Which.Name.Should().Be("Edward Norton");
        info.Crew.Should().ContainSingle().Which.Name.Should().Be("David Fincher");
        info.OriginalLanguage.Should().Be("en");
        info.Revenue.Should().Be(100853753);
        info.Budget.Should().Be(63000000);
    }

    [Fact]
    public async Task GetMovieInfoAsync_NullTmdbId_ThrowsArgumentException()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "{}"));
        var movie = new Movie("Unknown", 2020);

        var act = () => provider.GetMovieInfoAsync(movie);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetMovieInfoAsync_NullApiResponse_ThrowsInvalidOperationException()
    {
        var provider = CreateProvider(CreateMockHandler(HttpStatusCode.OK, "null"));
        var movie = new Movie("Fight Club", 1999, TmdbId: 550);

        var act = () => provider.GetMovieInfoAsync(movie);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchAsync_UsesCaching_SecondCallSkipsHttp()
    {
        const string json = """{"results":[{"id":550,"title":"Fight Club","originalTitle":"Fight Club","releaseDate":"1999-10-15","originalLanguage":"en"}]}""";
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, json);
        var cache = new MetadataCache(new MemoryCache(new MemoryCacheOptions()));
        var provider = CreateProvider(handler.Object, cache);

        await provider.SearchAsync("Fight Club");
        await provider.SearchAsync("Fight Club");

        handler.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}
