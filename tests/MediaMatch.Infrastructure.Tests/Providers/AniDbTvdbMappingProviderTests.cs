using System.Net;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class AniDbTvdbMappingProviderTests
{
    private const string MappingXml = @"<anime-list>
        <anime anidbid=""1"" tvdbid=""100"" />
        <anime anidbid=""2"" tvdbid=""200"" />
        <anime anidbid=""3"" tvdbid=""0"" />
        <anime anidbid=""bad"" tvdbid=""abc"" />
    </anime-list>";

    private static AniDbConfiguration DefaultConfig => new()
    {
        TvdbMappingUrl = "http://test.example/mapping.xml",
        MappingCacheHours = 24
    };

    private static (AniDbTvdbMappingProvider provider, Mock<IEpisodeProvider> tvdb) CreateProvider(
        string mappingXml = MappingXml)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mappingXml)
            });

        var httpClient = new HttpClient(handler.Object);
        var cache = new MetadataCache(new MemoryCache(new MemoryCacheOptions()));

        var tvdbProvider = new Mock<IEpisodeProvider>();
        tvdbProvider.Setup(p => p.Name).Returns("TVDb");
        tvdbProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Episode>
            {
                new("Mapped Show", 1, 1, "Episode 1")
            });
        tvdbProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeriesInfo("Mapped Show", "100", null, null, null, null, null, new List<string>()));

        var provider = new AniDbTvdbMappingProvider(
            httpClient, cache, DefaultConfig,
            new[] { tvdbProvider.Object },
            NullLogger<AniDbTvdbMappingProvider>.Instance);

        return (provider, tvdbProvider);
    }

    [Fact]
    public async Task MapAniDbToTvdbAsync_KnownId_ReturnsTvdbId()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.MapAniDbToTvdbAsync(1);
        result.Should().Be(100);
    }

    [Fact]
    public async Task MapAniDbToTvdbAsync_UnknownId_ReturnsNull()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.MapAniDbToTvdbAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task MapAniDbToTvdbAsync_ZeroTvdbId_ReturnsNull()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.MapAniDbToTvdbAsync(3); // tvdbid=0
        result.Should().BeNull();
    }

    [Fact]
    public async Task MapTvdbToAniDbAsync_KnownId_ReturnsAniDbId()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.MapTvdbToAniDbAsync(200);
        result.Should().Be(2);
    }

    [Fact]
    public async Task MapTvdbToAniDbAsync_UnknownId_ReturnsNull()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.MapTvdbToAniDbAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEpisodesViaTvdbFallbackAsync_WithMapping_ReturnsTvdbEpisodes()
    {
        var (provider, tvdb) = CreateProvider();
        var episodes = await provider.GetEpisodesViaTvdbFallbackAsync(1, "Test");
        episodes.Should().HaveCount(1);
        episodes[0].SeriesName.Should().Be("Mapped Show");
        tvdb.Verify(p => p.GetEpisodesAsync(
            It.Is<SearchResult>(s => s.Id == 100),
            It.IsAny<SortOrder>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEpisodesViaTvdbFallbackAsync_NoMapping_ReturnsEmpty()
    {
        var (provider, _) = CreateProvider();
        var episodes = await provider.GetEpisodesViaTvdbFallbackAsync(999, "NoMatch");
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeriesInfoViaTvdbFallbackAsync_WithMapping_ReturnsInfo()
    {
        var (provider, _) = CreateProvider();
        var info = await provider.GetSeriesInfoViaTvdbFallbackAsync(2, "Test");
        info.Should().NotBeNull();
        info!.Name.Should().Be("Mapped Show");
    }

    [Fact]
    public async Task GetSeriesInfoViaTvdbFallbackAsync_NoMapping_ReturnsNull()
    {
        var (provider, _) = CreateProvider();
        var info = await provider.GetSeriesInfoViaTvdbFallbackAsync(999, "Test");
        info.Should().BeNull();
    }

    [Fact]
    public async Task GetEpisodesViaTvdbFallbackAsync_NoTvdbProvider_ReturnsEmpty()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MappingXml)
            });

        var provider = new AniDbTvdbMappingProvider(
            new HttpClient(handler.Object),
            new MetadataCache(new MemoryCache(new MemoryCacheOptions())),
            DefaultConfig,
            Enumerable.Empty<IEpisodeProvider>(),
            NullLogger<AniDbTvdbMappingProvider>.Instance);

        var episodes = await provider.GetEpisodesViaTvdbFallbackAsync(1, "Test");
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task MappingCache_SecondCall_DoesNotRedownload()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MappingXml)
            });

        var provider = new AniDbTvdbMappingProvider(
            new HttpClient(handler.Object),
            new MetadataCache(new MemoryCache(new MemoryCacheOptions())),
            DefaultConfig,
            Enumerable.Empty<IEpisodeProvider>(),
            NullLogger<AniDbTvdbMappingProvider>.Instance);

        await provider.MapAniDbToTvdbAsync(1);
        await provider.MapAniDbToTvdbAsync(2);

        handler.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
