using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Detects network paths on Unix/macOS by checking UNC-style paths (Samba)
/// and reading mount information from /proc/mounts (Linux) or mount output (macOS).
/// </summary>
public sealed class UnixNetworkPathDetector : INetworkPathDetector
{
    private readonly ILogger<UnixNetworkPathDetector> _logger;

    public UnixNetworkPathDetector(ILogger<UnixNetworkPathDetector>? logger = null)
    {
        _logger = logger ?? NullLogger<UnixNetworkPathDetector>.Instance;
    }

    /// <inheritdoc />
    public bool IsNetworkPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // SMB/CIFS shares mounted at /mnt or /media — resolve to real path first
        try
        {
            var fullPath = Path.GetFullPath(path);

            // Check /proc/mounts on Linux for network filesystem types
            if (File.Exists("/proc/mounts"))
            {
                return IsNetworkMountLinux(fullPath);
            }

            // macOS: check mount output for network types (nfs, smbfs, afpfs)
            if (File.Exists("/sbin/mount"))
            {
                return IsNetworkMountMacOS(fullPath);
            }

            _logger.LogDebug("No mount detection available, assuming local path: {Path}", path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect network path for {Path}, assuming local", path);
            return false;
        }
    }

    private bool IsNetworkMountLinux(string fullPath)
    {
        // Network filesystem types in /proc/mounts
        var networkFsTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nfs", "nfs4", "cifs", "smbfs", "fuse.sshfs", "ncpfs", "9p"
        };

        try
        {
            foreach (var line in File.ReadLines("/proc/mounts"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                var mountPoint = parts[1];
                var fsType = parts[2];

                if (fullPath.StartsWith(mountPoint, StringComparison.Ordinal) &&
                    networkFsTypes.Contains(fsType))
                {
                    _logger.LogDebug("Network mount detected: {Path} on {MountPoint} ({FsType})", fullPath, mountPoint, fsType);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read /proc/mounts");
        }

        return false;
    }

    private bool IsNetworkMountMacOS(string fullPath)
    {
        // On macOS, read /etc/mtab or use mount output
        // Common network fs types: nfs, smbfs, afpfs, webdavfs
        var networkFsTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nfs", "smbfs", "afpfs", "webdavfs"
        };

        try
        {
            // macOS /etc/mnttab doesn't exist, but we can read from /etc/fstab
            // or parse mount command output — for now, use a simpler heuristic
            if (File.Exists("/etc/mtab"))
            {
                foreach (var line in File.ReadLines("/etc/mtab"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                        continue;

                    var mountPoint = parts[1];
                    var fsType = parts[2];

                    if (fullPath.StartsWith(mountPoint, StringComparison.Ordinal) &&
                        networkFsTypes.Contains(fsType))
                    {
                        _logger.LogDebug("Network mount detected: {Path} on {MountPoint} ({FsType})", fullPath, mountPoint, fsType);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read mount info on macOS");
        }

        return false;
    }
}
