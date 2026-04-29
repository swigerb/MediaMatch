using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Infrastructure.Persistence;
using Moq;

namespace MediaMatch.Infrastructure.Tests.Persistence;

/// <summary>
/// Passthrough encryption for testing — returns values unchanged.
/// </summary>
internal sealed class PassthroughEncryption : ISettingsEncryption
{
    public int EncryptCallCount { get; private set; }
    public int DecryptCallCount { get; private set; }

    public string Encrypt(string plainText)
    {
        EncryptCallCount++;
        return plainText;
    }

    public string Decrypt(string cipherText)
    {
        DecryptCallCount++;
        return cipherText;
    }

    public bool IsEncrypted(string value) => false;
}

public sealed class SettingsRepositoryTests
{
    [Fact]
    public void PassthroughEncryption_Encrypt_ReturnsOriginal()
    {
        var enc = new PassthroughEncryption();

        enc.Encrypt("api-key-123").Should().Be("api-key-123");
        enc.EncryptCallCount.Should().Be(1);
    }

    [Fact]
    public void PassthroughEncryption_Decrypt_ReturnsOriginal()
    {
        var enc = new PassthroughEncryption();

        enc.Decrypt("api-key-123").Should().Be("api-key-123");
        enc.DecryptCallCount.Should().Be(1);
    }

    [Fact]
    public void PassthroughEncryption_IsEncrypted_ReturnsFalse()
    {
        var enc = new PassthroughEncryption();

        enc.IsEncrypted("anything").Should().BeFalse();
    }

    [Fact]
    public void MockEncryption_EncryptDecrypt_CalledCorrectly()
    {
        var mock = new Mock<ISettingsEncryption>();
        mock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");
        mock.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s.Replace("ENC:", ""));
        mock.Setup(e => e.IsEncrypted(It.IsAny<string>())).Returns<string>(s => s.StartsWith("ENC:"));

        var encrypted = mock.Object.Encrypt("secret");
        var decrypted = mock.Object.Decrypt(encrypted);

        encrypted.Should().Be("ENC:secret");
        decrypted.Should().Be("secret");
        mock.Verify(e => e.Encrypt("secret"), Times.Once);
        mock.Verify(e => e.Decrypt("ENC:secret"), Times.Once);
    }

    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        settings.CacheDurationMinutes.Should().Be(60);
        settings.ApiKeys.Should().NotBeNull();
        settings.ApiKeys.TmdbApiKey.Should().BeEmpty();
        settings.ApiKeys.TvdbApiKey.Should().BeEmpty();
        settings.ApiKeys.OpenSubtitlesApiKey.Should().BeEmpty();
        settings.RenamePatterns.Should().NotBeNull();
        settings.OutputFolders.Should().NotBeNull();
    }

    // ── Real SettingsRepository round-trip integration tests ───────────────

    private static string CreateTempSettingsDir() =>
        Directory.CreateTempSubdirectory("mediamatch_settings_").FullName;

    private static void SafeDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); }
        catch { /* best effort cleanup */ }
    }

    [Fact]
    public async Task SaveLoad_RoundTrip_PreservesAllFields()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);

            var original = new AppSettings
            {
                CacheDurationMinutes = 1234,
                ThemeMode = ThemeMode.Dark,
                FontScale = FontScale.Large,
                EnableOpportunisticMode = false,
                MultiEpisodeNaming = MultiEpisodeNamingStrategy.Jellyfin,
                PreferLocalMetadata = false,
                MusicRenamePattern = "{artist}/{album}/{track}.{ext}",
            };
            original.ApiKeys.TmdbApiKey = "tmdb-secret";
            original.ApiKeys.TvdbApiKey = "tvdb-secret";
            original.ApiKeys.OpenSubtitlesApiKey = "os-secret";
            original.ApiKeys.AcoustIdApiKey = "acoust-secret";
            original.RenamePatterns.MoviePattern = "MOVIE-{n}";
            original.RenamePatterns.SeriesPattern = "SERIES-{n}";
            original.RenamePatterns.AnimePattern = "ANIME-{n}";
            original.OutputFolders.MoviesRoot = "/out/movies";
            original.OutputFolders.SeriesRoot = "/out/series";
            original.Presets.Add(new PresetDefinitionSettings
            {
                Name = "TV Plex",
                RenamePattern = "{n}/Season {s}/{t}",
                OutputFolder = "/plex/tv",
                Datasource = "tvdb",
                Language = "en",
                EpisodeOrder = "airdate",
                MatchMode = "strict",
                RenameActionType = RenameAction.Copy,
                InputFolder = "/input",
                IncludeFilter = "*.mkv",
                KeyboardShortcut = "Ctrl+1",
                PostActions = { "plex-refresh" },
            });
            original.Plex.Url = "http://plex:32400";
            original.Plex.Token = "plex-token";
            original.Plex.LibrarySectionIds.Add("1");
            original.Jellyfin.Url = "http://jelly:8096";
            original.Jellyfin.ApiKey = "jelly-key";
            original.PostProcessActions.Add(new PostProcessActionSettings
            {
                Name = "thumbnail",
                Enabled = true,
                Config = "high",
            });

            await repo.SaveAsync(original);
            var loaded = await repo.LoadAsync();

            loaded.CacheDurationMinutes.Should().Be(1234);
            loaded.ThemeMode.Should().Be(ThemeMode.Dark);
            loaded.FontScale.Should().Be(FontScale.Large);
            loaded.EnableOpportunisticMode.Should().BeFalse();
            loaded.MultiEpisodeNaming.Should().Be(MultiEpisodeNamingStrategy.Jellyfin);
            loaded.PreferLocalMetadata.Should().BeFalse();
            loaded.MusicRenamePattern.Should().Be("{artist}/{album}/{track}.{ext}");

            loaded.ApiKeys.TmdbApiKey.Should().Be("tmdb-secret");
            loaded.ApiKeys.TvdbApiKey.Should().Be("tvdb-secret");
            loaded.ApiKeys.OpenSubtitlesApiKey.Should().Be("os-secret");

            loaded.RenamePatterns.MoviePattern.Should().Be("MOVIE-{n}");
            loaded.RenamePatterns.SeriesPattern.Should().Be("SERIES-{n}");
            loaded.RenamePatterns.AnimePattern.Should().Be("ANIME-{n}");
            loaded.OutputFolders.MoviesRoot.Should().Be("/out/movies");
            loaded.OutputFolders.SeriesRoot.Should().Be("/out/series");

            loaded.Presets.Should().HaveCount(1);
            var preset = loaded.Presets[0];
            preset.Name.Should().Be("TV Plex");
            preset.RenamePattern.Should().Be("{n}/Season {s}/{t}");
            preset.OutputFolder.Should().Be("/plex/tv");
            preset.Datasource.Should().Be("tvdb");
            preset.MatchMode.Should().Be("strict");
            preset.RenameActionType.Should().Be(RenameAction.Copy);
            preset.PostActions.Should().ContainSingle().Which.Should().Be("plex-refresh");

            loaded.Plex.Url.Should().Be("http://plex:32400");
            loaded.Plex.Token.Should().Be("plex-token");
            loaded.Plex.LibrarySectionIds.Should().ContainSingle().Which.Should().Be("1");
            loaded.Jellyfin.Url.Should().Be("http://jelly:8096");
            loaded.Jellyfin.ApiKey.Should().Be("jelly-key");

            loaded.PostProcessActions.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { Name = "thumbnail", Enabled = true, Config = "high" });
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task SaveAsync_EncryptsApiKeysOnDisk_WithoutMutatingInMemorySettings()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            var encryption = new Mock<ISettingsEncryption>();
            encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");
            encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s =>
                s.StartsWith("ENC:") ? s["ENC:".Length..] : s);

            using var repo = new SettingsRepository(encryption.Object, dir);

            var settings = new AppSettings();
            settings.ApiKeys.TmdbApiKey = "tmdb-plain";

            await repo.SaveAsync(settings);

            // In-memory settings must NOT be mutated by save
            settings.ApiKeys.TmdbApiKey.Should().Be("tmdb-plain");

            // Raw file content should contain encrypted form
            var raw = await File.ReadAllTextAsync(Path.Combine(dir, "settings.json"));
            raw.Should().Contain("ENC:tmdb-plain");

            // Loaded settings should be decrypted
            var loaded = await repo.LoadAsync();
            loaded.ApiKeys.TmdbApiKey.Should().Be("tmdb-plain");
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsDefaults()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "settings.json"), "{ this is not : valid json ::}");

            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);

            var loaded = await repo.LoadAsync();

            loaded.Should().NotBeNull();
            loaded.CacheDurationMinutes.Should().Be(60); // default
            loaded.ApiKeys.TmdbApiKey.Should().BeEmpty();
            loaded.ThemeMode.Should().Be(ThemeMode.System);
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);

            repo.SettingsFileExists().Should().BeFalse();
            var loaded = await repo.LoadAsync();

            loaded.CacheDurationMinutes.Should().Be(60);
            loaded.Presets.Should().BeEmpty();
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTempFile()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);

            await repo.SaveAsync(new AppSettings { CacheDurationMinutes = 99 });

            File.Exists(Path.Combine(dir, "settings.json")).Should().BeTrue();
            File.Exists(Path.Combine(dir, "settings.json.tmp")).Should().BeFalse(
                "atomic write should rename the .tmp file, not leave it behind");
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);

            await repo.SaveAsync(new AppSettings { CacheDurationMinutes = 10 });
            await repo.SaveAsync(new AppSettings { CacheDurationMinutes = 20 });

            var loaded = await repo.LoadAsync();
            loaded.CacheDurationMinutes.Should().Be(20);
        }
        finally { SafeDelete(dir); }
    }

    [Fact]
    public async Task SettingsFileExists_TrueAfterSave()
    {
        var dir = CreateTempSettingsDir();
        try
        {
            using var repo = new SettingsRepository(new PassthroughEncryption(), dir);
            repo.SettingsFileExists().Should().BeFalse();

            await repo.SaveAsync(new AppSettings());

            repo.SettingsFileExists().Should().BeTrue();
        }
        finally { SafeDelete(dir); }
    }
}
