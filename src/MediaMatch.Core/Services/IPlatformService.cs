namespace MediaMatch.Core.Services;

/// <summary>
/// Provides platform detection and capability information for cross-platform support.
/// </summary>
public interface IPlatformService
{
    /// <summary>Gets the friendly name of the current platform (e.g. "Windows", "macOS", "Linux").</summary>
    string PlatformName { get; }

    /// <summary>Returns true when running on Windows.</summary>
    bool IsWindows { get; }

    /// <summary>Returns true when running on macOS.</summary>
    bool IsMacOS { get; }

    /// <summary>Returns true when running on Linux.</summary>
    bool IsLinux { get; }

    /// <summary>Returns true when the platform supports NTFS hard links (Windows) or POSIX hard links (macOS/Linux).</summary>
    bool SupportsHardLinks { get; }

    /// <summary>Returns true when the platform supports ReFS Copy-on-Write clone (Windows ReFS volumes only).</summary>
    bool SupportsReFsClone { get; }

    /// <summary>Gets the platform-appropriate settings directory path.</summary>
    string GetSettingsDirectory();
}
