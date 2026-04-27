using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Providers;

/// <summary>
/// E2E: MetadataProviderChain priority ordering, confidence short-circuit, and fallback behaviour.
/// </summary>
public class MetadataProviderChainE2ETests
{
    private static Episode MakeEpisode(string series, int season, int ep, string title) =>
        new(series, season, ep, title);

    private static SeriesInfo MakeSeriesInfo(string name) =>
        new(name, "1", null, null, null, null, null, []);

    private static Movie MakeMovie(string name, int year) =>
        new(name, year, TmdbId: 1);

    // ── Priority ordering ─────────────────────────────────────────────────

    [Fact]
    public async Task ProviderChain_LocalProviderFirst_WhenPreferLocalMetadata()
    {
        var nfoProvider = new Mock<IMovieProvider>();
        nfoProvider.Setup(p => p.Name).Returns("NFO");
        nfoProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { MakeMovie("Local Movie", 2020) });
        nfoProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("Local Movie", 2020, 1, null, null, null, null, null, null, null, [], [], []));

        var onlineProvider = new Mock<IMovieProvider>();
        onlineProvider.Setup(p => p.Name).Returns("TMDb");
        onlineProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { MakeMovie("Online Movie", 2021) });
        onlineProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("Online Movie", 2021, 2, null, null, null, null, null, null, null, [], [], []));

        var settings = new AppSettings { PreferLocalMetadata = true };
        var chain = new MetadataProviderChain(
            [nfoProvider.Object, onlineProvider.Object],
            [],
            settings);

        // NFO should appear first in the ordered list
        chain.MovieProviders.First().Name.Should().Be("NFO");
        chain.MovieProviders.Last().Name.Should().Be("TMDb");
    }

    [Fact]
    public async Task ProviderChain_OnlineProviderOrder_WhenPreferLocalFalse()
    {
        var nfoProvider = new Mock<IMovieProvider>();
        nfoProvider.Setup(p => p.Name).Returns("NFO");

        var tmdbProvider = new Mock<IMovieProvider>();
        tmdbProvider.Setup(p => p.Name).Returns("TMDb");

        var settings = new AppSettings { PreferLocalMetadata = false };
        var chain = new MetadataProviderChain(
            [nfoProvider.Object, tmdbProvider.Object],
            [],
            settings);

        // No reordering — original order preserved
        chain.MovieProviders[0].Name.Should().Be("NFO");
        chain.MovieProviders[1].Name.Should().Be("TMDb");
    }

    // ── Short-circuit at confidence threshold ─────────────────────────────

    [Fact]
    public async Task ProviderChain_LocalProvider_ShortCircuitsAt90Confidence()
    {
        var nfoProvider = new Mock<IMovieProvider>();
        nfoProvider.Setup(p => p.Name).Returns("NFO");
        nfoProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { MakeMovie("Inception", 2010) });
        nfoProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("Inception", 2010, 27205, null, null, null, null, null, null, null, [], [], []));

        var secondProvider = new Mock<IMovieProvider>();
        secondProvider.Setup(p => p.Name).Returns("TMDb");

        var chain = new MetadataProviderChain(
            [nfoProvider.Object, secondProvider.Object],
            [],
            new AppSettings { PreferLocalMetadata = true });

        // baseConfidence = 0.70 → local boost → 1.0 clamped to 0.95 ≥ 0.90 → short-circuit
        var result = await chain.MatchMovieAsync("Inception.2010.mkv", 0.70f);

        result.IsMatch.Should().BeTrue();
        result.ProviderSource.Should().Be("NFO");

        // Second provider should NEVER have been called
        secondProvider.Verify(
            p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProviderChain_LowConfidence_FallsBackToNextProvider()
    {
        var nfoProvider = new Mock<IMovieProvider>();
        nfoProvider.Setup(p => p.Name).Returns("NFO");
        nfoProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());  // no results

        var tmdbProvider = new Mock<IMovieProvider>();
        tmdbProvider.Setup(p => p.Name).Returns("TMDb");
        tmdbProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { MakeMovie("The Matrix", 1999) });
        tmdbProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("The Matrix", 1999, 603, null, null, null, null, null, null, null, [], [], []));

        var chain = new MetadataProviderChain(
            [nfoProvider.Object, tmdbProvider.Object],
            [],
            new AppSettings { PreferLocalMetadata = true });

        var result = await chain.MatchMovieAsync("The.Matrix.1999.mkv", 0.85f);

        result.IsMatch.Should().BeTrue();
        result.ProviderSource.Should().Be("TMDb");
    }

    [Fact]
    public async Task ProviderChain_AllProvidersFail_ReturnsNoMatch()
    {
        var p1 = new Mock<IMovieProvider>();
        p1.Setup(p => p.Name).Returns("TMDb");
        p1.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var p2 = new Mock<IMovieProvider>();
        p2.Setup(p => p.Name).Returns("TVDb");
        p2.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var chain = new MetadataProviderChain([p1.Object, p2.Object], [], new AppSettings());

        var result = await chain.MatchMovieAsync("Unknown.Movie.2025.mkv", 0.80f);

        result.IsMatch.Should().BeFalse();
        result.MediaType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public async Task ProviderChain_CancellationToken_StopsChain()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var provider = new Mock<IMovieProvider>();
        provider.Setup(p => p.Name).Returns("TMDb");

        var chain = new MetadataProviderChain([provider.Object], [], new AppSettings());

        var act = () => chain.MatchMovieAsync("test.mkv", 0.80f, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Episode chain ─────────────────────────────────────────────────────

    [Fact]
    public async Task EpisodeChain_LocalFirst_ShortCircuitsBeforeOnline()
    {
        var nfoProvider = new Mock<IEpisodeProvider>();
        nfoProvider.Setup(p => p.Name).Returns("NFO");
        nfoProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult> { new("Breaking Bad", 1) });
        nfoProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Episode> { MakeEpisode("Breaking Bad", 1, 1, "Pilot") });
        nfoProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSeriesInfo("Breaking Bad"));

        var onlineProvider = new Mock<IEpisodeProvider>();
        onlineProvider.Setup(p => p.Name).Returns("TVDb");

        var chain = new MetadataProviderChain(
            [],
            [nfoProvider.Object, onlineProvider.Object],
            new AppSettings { PreferLocalMetadata = true });

        // The NFO provider in the chain comes before TVDb
        chain.EpisodeProviders.First().Name.Should().Be("NFO");

        var result = await chain.MatchEpisodeAsync("Breaking.Bad.S01E01.mkv", 0.90f, MediaType.TvSeries);
        result.ProviderSource.Should().Be("NFO");
    }

    [Fact]
    public async Task EpisodeChain_NoProviders_ReturnsNoMatch()
    {
        var chain = new MetadataProviderChain([], [], new AppSettings());
        var result = await chain.MatchEpisodeAsync("test.mkv", 0.85f, MediaType.TvSeries);

        result.IsMatch.Should().BeFalse();
    }
}
