using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.FileSystem;

/// <summary>
/// Detects UNC paths and mapped network drives using the Win32 GetDriveType API.
/// When a network path is detected, callers should reduce concurrent I/O.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class NetworkPathDetector : INetworkPathDetector
{
    private const uint DRIVE_REMOTE = 4;

    private readonly ILogger<NetworkPathDetector> _logger;

    public NetworkPathDetector(ILogger<NetworkPathDetector>? logger = null)
    {
        _logger = logger ?? NullLogger<NetworkPathDetector>.Instance;
    }

    /// <inheritdoc />
    public bool IsNetworkPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // UNC paths are always network paths
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            _logger.LogDebug("UNC path detected: {Path}", path);
            return true;
        }

        // Check mapped drives via GetDriveType
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
                return false;

            uint driveType = NativeMethods.GetDriveType(root);
            bool isRemote = driveType == DRIVE_REMOTE;

            if (isRemote)
            {
                _logger.LogDebug("Mapped network drive detected: {Root}", root);
            }

            return isRemote;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect drive type for {Path}, assuming local", path);
            return false;
        }
    }

    private static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint GetDriveType(string lpRootPathName);
    }
}
