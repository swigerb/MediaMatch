using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private static readonly string DefaultSettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaMatch");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ISettingsEncryption _encryption;
    private readonly ILogger<SettingsRepository> _logger;
    private readonly string _settingsDir;
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    /// <param name="encryption">The encryption service used to protect API key values.</param>
    /// <param name="settingsDirectory">
    /// Optional override for the directory containing settings.json. Defaults to
    /// %LOCALAPPDATA%/MediaMatch. Used by tests to isolate file I/O.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics; defaults to <see cref="NullLogger{T}"/>.</param>
    public SettingsRepository(
        ISettingsEncryption encryption,
        string? settingsDirectory = null,
        ILogger<SettingsRepository>? logger = null)
    {
        _encryption = encryption;
        _settingsDir = settingsDirectory ?? DefaultSettingsDir;
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
        _logger = logger ?? NullLogger<SettingsRepository>.Instance;
    }

    /// <inheritdoc />
    public bool SettingsFileExists() => File.Exists(_settingsPath);

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Use FileShare.Read so CLI can read while App is open
            var stream = new FileStream(
                _settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (stream.ConfigureAwait(false))
            {
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct).ConfigureAwait(false);
                if (settings is null)
                    return new AppSettings();

                // Decrypt API keys after loading
                DecryptApiKeys(settings);
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
            Directory.CreateDirectory(_settingsDir);

            // Clone API keys so we encrypt without mutating the in-memory settings
            var clone = CloneSettings(settings);
            EncryptApiKeys(clone);

            // Write to a temp file first, then move — atomic on NTFS
            var tempPath = _settingsPath + ".tmp";
            var stream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, clone, JsonOptions, ct).ConfigureAwait(false);
            }

            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EncryptApiKeys(AppSettings settings)
    {
        var keys = settings.ApiKeys;
        keys.TmdbApiKey = _encryption.Encrypt(keys.TmdbApiKey);
        keys.TvdbApiKey = _encryption.Encrypt(keys.TvdbApiKey);
        keys.OpenSubtitlesApiKey = _encryption.Encrypt(keys.OpenSubtitlesApiKey);
        keys.AcoustIdApiKey = _encryption.Encrypt(keys.AcoustIdApiKey);

        settings.Plex.Token = _encryption.Encrypt(settings.Plex.Token);
        settings.Jellyfin.ApiKey = _encryption.Encrypt(settings.Jellyfin.ApiKey);

        var llm = settings.LlmSettings;
        llm.OpenAiApiKey = _encryption.Encrypt(llm.OpenAiApiKey);
        llm.AzureOpenAiApiKey = _encryption.Encrypt(llm.AzureOpenAiApiKey);
    }

    private void DecryptApiKeys(AppSettings settings)
    {
        var keys = settings.ApiKeys;
        keys.TmdbApiKey = SafeDecrypt(keys.TmdbApiKey, nameof(keys.TmdbApiKey));
        keys.TvdbApiKey = SafeDecrypt(keys.TvdbApiKey, nameof(keys.TvdbApiKey));
        keys.OpenSubtitlesApiKey = SafeDecrypt(keys.OpenSubtitlesApiKey, nameof(keys.OpenSubtitlesApiKey));
        keys.AcoustIdApiKey = SafeDecrypt(keys.AcoustIdApiKey, nameof(keys.AcoustIdApiKey));

        settings.Plex.Token = SafeDecrypt(settings.Plex.Token, "Plex.Token");
        settings.Jellyfin.ApiKey = SafeDecrypt(settings.Jellyfin.ApiKey, "Jellyfin.ApiKey");

        var llm = settings.LlmSettings;
        llm.OpenAiApiKey = SafeDecrypt(llm.OpenAiApiKey, nameof(llm.OpenAiApiKey));
        llm.AzureOpenAiApiKey = SafeDecrypt(llm.AzureOpenAiApiKey, nameof(llm.AzureOpenAiApiKey));
    }

    /// <summary>
    /// Decrypts a value, returning empty string if the ciphertext is corrupt.
    /// Treats <see cref="FormatException"/> (bad Base64) and
    /// <see cref="System.Security.Cryptography.CryptographicException"/> (wrong DPAPI scope or
    /// tampered ciphertext) as recoverable: the affected key is reset rather than crashing
    /// the whole settings load.
    /// </summary>
    private string SafeDecrypt(string cipherText, string fieldName)
    {
        try
        {
            return _encryption.Decrypt(cipherText);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Encrypted setting {Field} has invalid Base64; resetting to empty.", fieldName);
            return string.Empty;
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            _logger.LogWarning(ex, "Encrypted setting {Field} could not be decrypted (different user/machine?); resetting to empty.", fieldName);
            return string.Empty;
        }
    }

    private static AppSettings CloneSettings(AppSettings source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();
}
