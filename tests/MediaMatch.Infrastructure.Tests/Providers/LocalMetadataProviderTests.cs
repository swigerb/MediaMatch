using FluentAssertions;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Providers;

namespace MediaMatch.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for NfoMetadataProvider and XmlMetadataProvider non-file-I/O methods.
/// File-based methods require actual sidecar files on disk.
/// </summary>
public sealed class LocalMetadataProviderTests
{
    // ── NfoMetadataProvider ──────────────────────────────────────

    [Fact]
    public void NfoMetadataProvider_Name_IsNFO()
    {
        var provider = new NfoMetadataProvider();
        provider.Name.Should().Be("NFO");
    }

    [Fact]
    public async Task NfoMetadataProvider_MovieSearchAsync_ReturnsEmpty()
    {
        IMovieProvider provider = new NfoMetadataProvider();
        var results = await provider.SearchAsync("any query");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task NfoMetadataProvider_MovieSearchAsync_WithYear_ReturnsEmpty()
    {
        IMovieProvider provider = new NfoMetadataProvider();
        var results = await provider.SearchAsync("movie", 2024);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task NfoMetadataProvider_GetMovieInfoAsync_ReturnsMinimalInfo()
    {
        var provider = new NfoMetadataProvider();
        var movie = new Movie("Test Movie", 2024);
        var info = await provider.GetMovieInfoAsync(movie);

        info.Name.Should().Be("Test Movie");
        info.Year.Should().Be(2024);
        info.Genres.Should().BeEmpty();
        info.Cast.Should().BeEmpty();
        info.Crew.Should().BeEmpty();
    }

    [Fact]
    public async Task NfoMetadataProvider_EpisodeSearchAsync_ReturnsEmpty()
    {
        IEpisodeProvider provider = new NfoMetadataProvider();
        var results = await provider.SearchAsync("any show");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task NfoMetadataProvider_GetEpisodesAsync_ReturnsEmpty()
    {
        var provider = new NfoMetadataProvider();
        var series = new SearchResult("Test Show", 1);
        var episodes = await provider.GetEpisodesAsync(series);
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task NfoMetadataProvider_GetSeriesInfoAsync_ReturnsMinimalInfo()
    {
        var provider = new NfoMetadataProvider();
        var series = new SearchResult("Test Show", 1);
        var info = await provider.GetSeriesInfoAsync(series);

        info.Name.Should().Be("Test Show");
    }

    [Fact]
    public async Task NfoMetadataProvider_SearchByFile_NonexistentPath_ReturnsEmpty()
    {
        var provider = new NfoMetadataProvider();
        var results = await provider.SearchByFileAsync(@"C:\nonexistent\path\movie.mkv");
        results.Should().BeEmpty();
    }

    // ── XmlMetadataProvider ─────────────────────────────────────

    [Fact]
    public void XmlMetadataProvider_Name_IsXML()
    {
        var provider = new XmlMetadataProvider();
        provider.Name.Should().Be("XML");
    }

    [Fact]
    public async Task XmlMetadataProvider_MovieSearchAsync_ReturnsEmpty()
    {
        IMovieProvider provider = new XmlMetadataProvider();
        var results = await provider.SearchAsync("any query");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task XmlMetadataProvider_MovieSearchAsync_WithYear_ReturnsEmpty()
    {
        IMovieProvider provider = new XmlMetadataProvider();
        var results = await provider.SearchAsync("movie", 2024);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task XmlMetadataProvider_GetMovieInfoAsync_ReturnsMinimalInfo()
    {
        var provider = new XmlMetadataProvider();
        var movie = new Movie("Another Movie", 2023);
        var info = await provider.GetMovieInfoAsync(movie);

        info.Name.Should().Be("Another Movie");
        info.Year.Should().Be(2023);
    }

    [Fact]
    public async Task XmlMetadataProvider_GetEpisodesAsync_ReturnsEmpty()
    {
        var provider = new XmlMetadataProvider();
        var series = new SearchResult("Test Series", 1);
        var episodes = await provider.GetEpisodesAsync(series);
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task XmlMetadataProvider_GetSeriesInfoAsync_ReturnsMinimalInfo()
    {
        var provider = new XmlMetadataProvider();
        var series = new SearchResult("Test Series", 1);
        var info = await provider.GetSeriesInfoAsync(series);

        info.Name.Should().Be("Test Series");
    }

    [Fact]
    public async Task XmlMetadataProvider_SearchByFile_NonexistentPath_ReturnsEmpty()
    {
        var provider = new XmlMetadataProvider();
        var results = await provider.SearchByFileAsync(@"C:\nonexistent\path\movie.mkv");
        results.Should().BeEmpty();
    }
}
