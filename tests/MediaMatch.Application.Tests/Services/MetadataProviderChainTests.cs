using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Moq;

namespace MediaMatch.Application.Tests.Services;

public sealed class MetadataProviderChainTests
{
    private static Mock<IMovieProvider> CreateMovieProvider(
        string name, IReadOnlyList<Movie>? movies = null, MovieInfo? movieInfo = null)
    {
        var mock = new Mock<IMovieProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies ?? new List<Movie> { new("Movie", 2024) });
        mock.Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movieInfo ?? new MovieInfo("Movie", 2024, null, null, null, null, null, null, null, null, [], [], []));
        return mock;
    }

    private static Mock<T> CreateLocalMovieProvider<T>(string name)
        where T : class, IMovieProvider, ILocalMetadataProvider
    {
        var mock = new Mock<T>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.SearchByFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { new("Local Movie", 2024) });
        mock.Setup(p => p.GetMovieInfoByFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("Local Movie", 2024, null, null, "Overview", null, null, null, null, null, [], [], []));
        mock.Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo("Local Movie", 2024, null, null, null, null, null, null, null, null, [], [], []));
        mock.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Movie>());
        return mock;
    }

    public interface INfoMovieProvider : IMovieProvider, ILocalMetadataProvider { }
    public interface IXmlMovieProvider : IMovieProvider, ILocalMetadataProvider { }

    [Fact]
    public void MovieProviders_WithPreferLocal_LocalProvidersFirst()
    {
        var online = CreateMovieProvider("TMDb");
        var local = CreateLocalMovieProvider<INfoMovieProvider>("NFO");

        var chain = new MetadataProviderChain(
            new IMovieProvider[] { online.Object, local.Object },
            Array.Empty<IEpisodeProvider>(),
            new AppSettings { PreferLocalMetadata = true });

        chain.MovieProviders[0].Name.Should().Be("NFO");
        chain.MovieProviders[1].Name.Should().Be("TMDb");
    }

    [Fact]
    public void MovieProviders_WithoutPreferLocal_OriginalOrder()
    {
        var tmdb = CreateMovieProvider("TMDb");
        var local = CreateLocalMovieProvider<INfoMovieProvider>("NFO");

        var chain = new MetadataProviderChain(
            new IMovieProvider[] { tmdb.Object, local.Object },
            Array.Empty<IEpisodeProvider>(),
            new AppSettings { PreferLocalMetadata = false });

        chain.MovieProviders[0].Name.Should().Be("TMDb");
        chain.MovieProviders[1].Name.Should().Be("NFO");
    }

    [Fact]
    public async Task MatchMovieAsync_LocalProvider_BoostsConfidence()
    {
        var local = CreateLocalMovieProvider<INfoMovieProvider>("NFO");
        var chain = new MetadataProviderChain(
            new IMovieProvider[] { local.Object },
            Array.Empty<IEpisodeProvider>(),
            new AppSettings { PreferLocalMetadata = true });

        var result = await chain.MatchMovieAsync(@"C:\movie.mkv", 0.50f);

        result.IsMatch.Should().BeTrue();
        result.Confidence.Should().BeGreaterThan(0.50f); // NFO gets +0.30 boost
        result.ProviderSource.Should().Be("NFO");
    }

    [Fact]
    public async Task MatchMovieAsync_NoProviderFindsMovie_ReturnsNoMatch()
    {
        var empty = new Mock<IMovieProvider>();
        empty.Setup(p => p.Name).Returns("Empty");
        empty.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Movie>());

        var chain = new MetadataProviderChain(
            new[] { empty.Object },
            Array.Empty<IEpisodeProvider>());

        var result = await chain.MatchMovieAsync(@"C:\movie.mkv", 0.70f);
        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task MatchMovieAsync_ShortCircuitsOnHighConfidence()
    {
        var local = CreateLocalMovieProvider<INfoMovieProvider>("NFO");
        var online = CreateMovieProvider("TMDb");

        var chain = new MetadataProviderChain(
            new IMovieProvider[] { local.Object, online.Object },
            Array.Empty<IEpisodeProvider>(),
            new AppSettings { PreferLocalMetadata = true });

        var result = await chain.MatchMovieAsync(@"C:\movie.mkv", 0.70f);

        // NFO: 0.70 + 0.30 = 0.95 >= 0.90 threshold → short-circuit
        result.ProviderSource.Should().Be("NFO");
        online.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchMovieAsync_ProviderThrows_ContinuesToNext()
    {
        var failing = new Mock<IMovieProvider>();
        failing.Setup(p => p.Name).Returns("Failing");
        failing.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var working = CreateMovieProvider("Working");
        var chain = new MetadataProviderChain(
            new[] { failing.Object, working.Object },
            Array.Empty<IEpisodeProvider>());

        var result = await chain.MatchMovieAsync(@"C:\movie.mkv", 0.90f);
        result.IsMatch.Should().BeTrue();
        result.ProviderSource.Should().Be("Working");
    }

    [Fact]
    public async Task MatchEpisodeAsync_NoMatch_ReturnsNoMatch()
    {
        var empty = new Mock<IEpisodeProvider>();
        empty.Setup(p => p.Name).Returns("Empty");
        empty.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SearchResult>());

        var chain = new MetadataProviderChain(
            Array.Empty<IMovieProvider>(),
            new[] { empty.Object });

        var result = await chain.MatchEpisodeAsync(@"C:\show.mkv", 0.70f, MediaType.TvSeries);
        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task MatchEpisodeAsync_OnlineProvider_ReturnsMatch()
    {
        var online = new Mock<IEpisodeProvider>();
        online.Setup(p => p.Name).Returns("TMDb");
        online.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult> { new("Show", 1) });
        online.Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeriesInfo("Show", "1", null, null, null, null, null, []));

        var chain = new MetadataProviderChain(
            Array.Empty<IMovieProvider>(),
            new[] { online.Object });

        var result = await chain.MatchEpisodeAsync(@"C:\show.mkv", 0.90f, MediaType.TvSeries);
        result.IsMatch.Should().BeTrue();
        result.ProviderSource.Should().Be("TMDb");
    }
}
