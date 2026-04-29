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

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    /// <param name="encryption">The encryption service used to protect API key values.</param>
    public SettingsRepository(ISettingsEncryption encryption)
    {
        _encryption = encryption;
    }

    /// <inheritdoc />
    public bool SettingsFileExists() => File.Exists(SettingsPath);

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Use FileShare.Read so CLI can read while App is open
            var stream = new FileStream(
                SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (stream.ConfigureAwait(false))
            {
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct).ConfigureAwait(false);
                if (settings is null)
                    return new AppSettings();

                // Decrypt API keys after loading
                DecryptApiKeys(settings.ApiKeys);
                return settings;
            }
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

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(SettingsDir);

            // Clone API keys so we encrypt without mutating the in-memory settings
            var clone = CloneSettings(settings);
            EncryptApiKeys(clone.ApiKeys);

            // Write to a temp file first, then move — atomic on NTFS
            var tempPath = SettingsPath + ".tmp";
            var stream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, clone, JsonOptions, ct).ConfigureAwait(false);
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
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();
}
