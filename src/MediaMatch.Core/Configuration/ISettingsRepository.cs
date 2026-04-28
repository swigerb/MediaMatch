namespace MediaMatch.Core.Configuration;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> to persistent storage.
/// Implementations live in Infrastructure; consumers depend on this interface.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>Loads settings from disk, falling back to defaults on error.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The loaded application settings.</returns>
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>Persists settings to disk in a thread-safe manner.</summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);

    /// <summary>Returns true if the settings file exists on disk.</summary>
    /// <returns>A value indicating whether the settings file exists.</returns>
    bool SettingsFileExists();
}
