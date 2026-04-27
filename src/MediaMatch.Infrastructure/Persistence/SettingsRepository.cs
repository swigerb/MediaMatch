using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;

namespace MediaMatch.Infrastructure.Persistence;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> to a JSON file at
/// %LOCALAPPDATA%/MediaMatch/settings.json.
/// Thread-safe via a <see cref="SemaphoreSlim"/>; gracefully handles
/// corrupt or missing files by falling back to defaults.
/// API key values are encrypted/decrypted on save/load via <see cref="ISettingsEncryption"/>.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository, IDisposable
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaMatch");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ISettingsEncryption _encryption;

    public SettingsRepository(ISettingsEncryption encryption)
    {
        _encryption = encryption;
    }

    public bool SettingsFileExists() => File.Exists(SettingsPath);

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        await _lock.WaitAsync(ct);
        try
        {
            // Use FileShare.Read so CLI can read while App is open
            await using var stream = new FileStream(
                SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct);
            if (settings is null)
                return new AppSettings();

            // Decrypt API keys after loading
            DecryptApiKeys(settings.ApiKeys);
            return settings;
        }
        catch (JsonException)
        {
            // Corrupt file — return defaults
            return new AppSettings();
        }
        catch (IOException)
        {
            // File locked by another process — return defaults
            return new AppSettings();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(SettingsDir);

            // Clone API keys so we encrypt without mutating the in-memory settings
            var clone = CloneSettings(settings);
            EncryptApiKeys(clone.ApiKeys);

            // Write to a temp file first, then move — atomic on NTFS
            var tempPath = SettingsPath + ".tmp";
            await using (var stream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, clone, JsonOptions, ct);
            }

            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EncryptApiKeys(ApiKeySettings keys)
    {
        keys.TmdbApiKey = _encryption.Encrypt(keys.TmdbApiKey);
        keys.TvdbApiKey = _encryption.Encrypt(keys.TvdbApiKey);
        keys.OpenSubtitlesApiKey = _encryption.Encrypt(keys.OpenSubtitlesApiKey);
    }

    private void DecryptApiKeys(ApiKeySettings keys)
    {
        keys.TmdbApiKey = _encryption.Decrypt(keys.TmdbApiKey);
        keys.TvdbApiKey = _encryption.Decrypt(keys.TvdbApiKey);
        keys.OpenSubtitlesApiKey = _encryption.Decrypt(keys.OpenSubtitlesApiKey);
    }

    private static AppSettings CloneSettings(AppSettings source)
    {
        return new AppSettings
        {
            CacheDurationMinutes = source.CacheDurationMinutes,
            ApiKeys = new ApiKeySettings
            {
                TmdbApiKey = source.ApiKeys.TmdbApiKey,
                TvdbApiKey = source.ApiKeys.TvdbApiKey,
                OpenSubtitlesApiKey = source.ApiKeys.OpenSubtitlesApiKey
            },
            RenamePatterns = new RenameSettings
            {
                MoviePattern = source.RenamePatterns.MoviePattern,
                SeriesPattern = source.RenamePatterns.SeriesPattern,
                AnimePattern = source.RenamePatterns.AnimePattern
            },
            OutputFolders = new OutputFolderSettings
            {
                MoviesRoot = source.OutputFolders.MoviesRoot,
                SeriesRoot = source.OutputFolders.SeriesRoot
            }
        };
    }

    public void Dispose() => _lock.Dispose();
}
