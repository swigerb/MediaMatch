using FluentAssertions;
using MediaMatch.Core.Configuration;

namespace MediaMatch.Core.Tests.Models;

public sealed class LlmConfigurationTests
{
    [Fact]
    public void DefaultValues_AreSet()
    {
        var config = new LlmConfiguration();
        config.Provider.Should().Be(LlmProviderType.None);
        config.OpenAiModel.Should().Be("gpt-4o");
        config.OllamaEndpoint.Should().Be("http://localhost:11434");
        config.OllamaModel.Should().Be("llama3");
        config.MaxTokens.Should().Be(500);
        config.TimeoutSeconds.Should().Be(30);
        config.SystemPrompt.Should().NotBeEmpty();
        config.AzureOpenAiApiVersion.Should().Be("2024-02-01");
    }

    [Fact]
    public void LlmProviderType_Values()
    {
        ((int)LlmProviderType.None).Should().Be(0);
        ((int)LlmProviderType.OpenAI).Should().Be(1);
        ((int)LlmProviderType.AzureOpenAI).Should().Be(2);
        ((int)LlmProviderType.Ollama).Should().Be(3);
    }
}

public sealed class AniDbConfigurationTests
{
    [Fact]
    public void DefaultValues_AreSet()
    {
        var config = new AniDbConfiguration();
        config.BaseUrl.Should().Contain("anidb.net");
        config.RateLimitIntervalMs.Should().Be(2100);
        config.MappingCacheHours.Should().Be(24);
        config.MaxRetries.Should().Be(3);
        config.TimeoutSeconds.Should().Be(30);
        config.ProtocolVersion.Should().Be(1);
        config.ClientVersion.Should().Be(1);
    }

    [Fact]
    public void TvdbMappingUrl_PointsToAnimeListsRepo()
    {
        var config = new AniDbConfiguration();
        config.TvdbMappingUrl.Should().Contain("anime-list");
    }
}

public sealed class PerformanceSettingsTests
{
    [Fact]
    public void DefaultValues_AreReasonable()
    {
        var settings = new PerformanceSettings();
        settings.MaxScanThreads.Should().BeGreaterThan(0);
        settings.NetworkConcurrency.Should().Be(2);
        settings.MaxDirectoryDepth.Should().Be(20);
        settings.EnableLazyMetadata.Should().BeTrue();
    }
}

public sealed class AppSettingsNewFeaturesTests
{
    [Fact]
    public void EnableOpportunisticMode_DefaultTrue()
    {
        var settings = new AppSettings();
        settings.EnableOpportunisticMode.Should().BeTrue();
    }

    [Fact]
    public void PreferLocalMetadata_DefaultTrue()
    {
        var settings = new AppSettings();
        settings.PreferLocalMetadata.Should().BeTrue();
    }

    [Fact]
    public void MultiEpisodeNaming_DefaultPlex()
    {
        var settings = new AppSettings();
        settings.MultiEpisodeNaming.Should().Be(MultiEpisodeNamingStrategy.Plex);
    }

    [Fact]
    public void MusicRenamePattern_HasDefault()
    {
        var settings = new AppSettings();
        settings.MusicRenamePattern.Should().Contain("{albumartist}");
        settings.MusicRenamePattern.Should().Contain("{track");
    }

    [Fact]
    public void PostProcessActions_DefaultEmpty()
    {
        var settings = new AppSettings();
        settings.PostProcessActions.Should().BeEmpty();
    }

    [Fact]
    public void Presets_DefaultEmpty()
    {
        var settings = new AppSettings();
        settings.Presets.Should().BeEmpty();
    }

    [Fact]
    public void PlexSettings_DefaultEmpty()
    {
        var settings = new AppSettings();
        settings.Plex.Url.Should().BeEmpty();
        settings.Plex.Token.Should().BeEmpty();
        settings.Plex.LibrarySectionIds.Should().BeEmpty();
    }

    [Fact]
    public void JellyfinSettings_DefaultEmpty()
    {
        var settings = new AppSettings();
        settings.Jellyfin.Url.Should().BeEmpty();
        settings.Jellyfin.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void PresetDefinitionSettings_Properties()
    {
        var preset = new PresetDefinitionSettings
        {
            Name = "TV → Plex",
            RenamePattern = "{SeriesName}/S{Season:D2}E{Episode:D2}.mkv",
            OutputFolder = @"C:\Media\TV",
            PostActions = new List<string> { "plex-refresh" }
        };

        preset.Name.Should().Be("TV → Plex");
        preset.PostActions.Should().Contain("plex-refresh");
    }

    [Fact]
    public void MultiEpisodeNamingStrategy_AllValues()
    {
        ((int)MultiEpisodeNamingStrategy.Plex).Should().Be(0);
        ((int)MultiEpisodeNamingStrategy.Jellyfin).Should().Be(1);
        ((int)MultiEpisodeNamingStrategy.Custom).Should().Be(2);
    }

    [Fact]
    public void PostProcessActionSettings_Properties()
    {
        var action = new PostProcessActionSettings
        {
            Name = "plex-refresh",
            Enabled = true,
            Config = null
        };
        action.Name.Should().Be("plex-refresh");
        action.Enabled.Should().BeTrue();
    }

    [Fact]
    public void LlmSettings_DefaultNone()
    {
        var settings = new AppSettings();
        settings.LlmSettings.Provider.Should().Be(LlmProviderType.None);
    }
}
