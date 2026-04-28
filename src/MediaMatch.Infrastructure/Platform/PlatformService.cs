using MediaMatch.Core.Services;

namespace MediaMatch.Infrastructure.Platform;

/// <summary>
/// Provides runtime platform detection and capability information.
/// </summary>
public sealed class PlatformService : IPlatformService
{
    /// <inheritdoc />
    public string PlatformName =>
        IsWindows ? "Windows" :
        IsMacOS ? "macOS" :
        IsLinux ? "Linux" :
        "Unknown";

    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsMacOS => OperatingSystem.IsMacOS();

    /// <inheritdoc />
    public bool IsLinux => OperatingSystem.IsLinux();

    /// <inheritdoc />
    public bool SupportsHardLinks => IsWindows || IsMacOS || IsLinux;

    /// <inheritdoc />
    public bool SupportsReFsClone => IsWindows;

    /// <inheritdoc />
    public string GetSettingsDirectory()
    {
        if (IsWindows)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MediaMatch");
        }

        if (IsMacOS)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "MediaMatch");
        }

        // Linux: respect XDG_CONFIG_HOME, default to ~/.config
        var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfig))
        {
            return Path.Combine(xdgConfig, "MediaMatch");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "MediaMatch");
    }
}
