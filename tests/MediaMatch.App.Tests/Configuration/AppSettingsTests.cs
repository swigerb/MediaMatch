using FluentAssertions;
using MediaMatch.Core.Configuration;

namespace MediaMatch.App.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        settings.CacheDurationMinutes.Should().Be(60);
        settings.ApiKeys.Should().NotBeNull();
        settings.RenamePatterns.Should().NotBeNull();
        settings.OutputFolders.Should().NotBeNull();
    }

    [Fact]
    public void ApiKeySettings_DefaultValues_AreEmpty()
    {
        var keys = new ApiKeySettings();

        keys.TmdbApiKey.Should().BeEmpty();
        keys.TvdbApiKey.Should().BeEmpty();
        keys.OpenSubtitlesApiKey.Should().BeEmpty();
    }

    [Fact]
    public void RenameSettings_DefaultPatterns_ContainExpectedTokens()
    {
        var rename = new RenameSettings();

        rename.MoviePattern.Should().Contain("{Name}").And.Contain("{Year}");
        rename.SeriesPattern.Should().Contain("{SeriesName}").And.Contain("{Season}").And.Contain("Episode").And.Contain("{Title}");
        rename.AnimePattern.Should().Contain("{SeriesName}").And.Contain("{Season}").And.Contain("Episode").And.Contain("{Title}");
    }

    [Fact]
    public void ApiConfiguration_DefaultValues_AreCorrect()
    {
        var config = new ApiConfiguration();

        config.TmdbBaseUrl.Should().Be("https://api.themoviedb.org/3");
        config.TvdbBaseUrl.Should().Be("https://api4.thetvdb.com/v4");
        config.Language.Should().Be("en-US");
        config.TimeoutSeconds.Should().Be(30);
        config.MaxRetries.Should().Be(3);
        config.CacheTtlMinutes.Should().Be(60);
        config.TmdbApiKey.Should().BeEmpty();
        config.TvdbApiKey.Should().BeEmpty();
        config.TmdbImageBaseUrl.Should().Be("https://image.tmdb.org/t/p/original");
    }

    [Fact]
    public void OutputFolderSettings_DefaultValues_AreEmpty()
    {
        var folders = new OutputFolderSettings();

        folders.MoviesRoot.Should().BeEmpty();
        folders.SeriesRoot.Should().BeEmpty();
    }

    [Fact]
    public void AppSettings_CanSetAllProperties()
    {
        var settings = new AppSettings
        {
            CacheDurationMinutes = 120,
            ApiKeys = new ApiKeySettings
            {
                TmdbApiKey = "tmdb-key",
                TvdbApiKey = "tvdb-key",
                OpenSubtitlesApiKey = "os-key"
            },
            RenamePatterns = new RenameSettings
            {
                MoviePattern = "custom-movie",
                SeriesPattern = "custom-series",
                AnimePattern = "custom-anime"
            },
            OutputFolders = new OutputFolderSettings
            {
                MoviesRoot = @"C:\Movies",
                SeriesRoot = @"C:\Series"
            }
        };

        settings.CacheDurationMinutes.Should().Be(120);
        settings.ApiKeys.TmdbApiKey.Should().Be("tmdb-key");
        settings.ApiKeys.TvdbApiKey.Should().Be("tvdb-key");
        settings.ApiKeys.OpenSubtitlesApiKey.Should().Be("os-key");
        settings.RenamePatterns.MoviePattern.Should().Be("custom-movie");
        settings.RenamePatterns.SeriesPattern.Should().Be("custom-series");
        settings.RenamePatterns.AnimePattern.Should().Be("custom-anime");
        settings.OutputFolders.MoviesRoot.Should().Be(@"C:\Movies");
        settings.OutputFolders.SeriesRoot.Should().Be(@"C:\Series");
    }
}
