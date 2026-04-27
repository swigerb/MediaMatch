namespace MediaMatch.Core.Configuration;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> to persistent storage.
/// Implementations live in Infrastructure; consumers depend on this interface.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>Loads settings from disk, falling back to defaults on error.</summary>
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>Persists settings to disk in a thread-safe manner.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);

    /// <summary>Returns true if the settings file exists on disk.</summary>
    bool SettingsFileExists();
}
