using MediaMatch.Core.Services;

namespace MediaMatch.Infrastructure.Platform;

/// <summary>
/// Provides runtime platform detection and capability information.
/// </summary>
public sealed class PlatformService : IPlatformService
{
    public string PlatformName =>
        IsWindows ? "Windows" :
        IsMacOS ? "macOS" :
        IsLinux ? "Linux" :
        "Unknown";

    public bool IsWindows => OperatingSystem.IsWindows();

    public bool IsMacOS => OperatingSystem.IsMacOS();

    public bool IsLinux => OperatingSystem.IsLinux();

    /// <summary>
    /// Hard links are supported on Windows (NTFS), macOS (APFS/HFS+), and Linux (ext4/Btrfs/XFS).
    /// </summary>
    public bool SupportsHardLinks => IsWindows || IsMacOS || IsLinux;

    /// <summary>
    /// ReFS CoW clone is a Windows-only feature available on ReFS volumes.
    /// </summary>
    public bool SupportsReFsClone => IsWindows;

    /// <summary>
    /// Returns the platform-appropriate settings directory:
    /// <list type="bullet">
    ///   <item>Windows: %LOCALAPPDATA%\MediaMatch</item>
    ///   <item>macOS: ~/Library/Application Support/MediaMatch</item>
    ///   <item>Linux: ~/.config/MediaMatch (XDG_CONFIG_HOME)</item>
    /// </list>
    /// </summary>
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
