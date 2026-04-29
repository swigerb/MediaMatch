using System.Diagnostics;
using System.Text.RegularExpressions;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Detects network paths on Unix/macOS by reading mount information from
/// /proc/mounts (Linux) or by parsing the output of /sbin/mount (macOS).
/// </summary>
public sealed partial class UnixNetworkPathDetector : INetworkPathDetector
{
    private static readonly HashSet<string> LinuxNetworkFsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nfs", "nfs4", "cifs", "smbfs", "fuse.sshfs", "ncpfs", "9p",
    };

    private static readonly HashSet<string> MacNetworkFsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nfs", "smbfs", "afpfs", "webdav", "webdavfs",
    };

    // Matches macOS `mount` output:  <device> on <mount-point> (<fstype>, <opts>)
    [GeneratedRegex(@"^(?<device>.+?)\s+on\s+(?<mount>.+?)\s+\((?<fstype>[^,\)]+)", RegexOptions.Compiled)]
    private static partial Regex MacMountLineRegex();

    private readonly ILogger<UnixNetworkPathDetector> _logger;

    public UnixNetworkPathDetector(ILogger<UnixNetworkPathDetector>? logger = null)
    {
        _logger = logger ?? NullLogger<UnixNetworkPathDetector>.Instance;
    }

    /// <inheritdoc />
    public bool IsNetworkPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists("/proc/mounts"))
                return IsNetworkMountLinux(fullPath);

            if (OperatingSystem.IsMacOS() && File.Exists("/sbin/mount"))
                return IsNetworkMountMacOS(fullPath);

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
                    LinuxNetworkFsTypes.Contains(fsType))
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
        // Run `/sbin/mount` and parse each line. Output looks like:
        //   /dev/disk1s5 on / (apfs, local, journaled)
        //   //user@server/share on /Volumes/share (smbfs, nodev, nosuid, mounted by user)
        string mountOutput;
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/sbin/mount",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            proc.Start();
            mountOutput = proc.StandardOutput.ReadToEnd();

            if (!proc.WaitForExit(5_000))
            {
                _logger.LogDebug("/sbin/mount timed out");
                try { proc.Kill(); } catch { /* best-effort */ }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to invoke /sbin/mount");
            return false;
        }

        string? bestMatch = null;
        var bestLength = 0;
        var matchedFsType = string.Empty;

        foreach (var line in mountOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = MacMountLineRegex().Match(line);
            if (!match.Success)
                continue;

            var mountPoint = match.Groups["mount"].Value.Trim();
            var fsType = match.Groups["fstype"].Value.Trim();

            if (fullPath.StartsWith(mountPoint, StringComparison.Ordinal) &&
                mountPoint.Length > bestLength)
            {
                bestMatch = mountPoint;
                bestLength = mountPoint.Length;
                matchedFsType = fsType;
            }
        }

        if (bestMatch is not null && MacNetworkFsTypes.Contains(matchedFsType))
        {
            _logger.LogDebug("Network mount detected: {Path} on {MountPoint} ({FsType})", fullPath, bestMatch, matchedFsType);
            return true;
        }

        return false;
    }
}
