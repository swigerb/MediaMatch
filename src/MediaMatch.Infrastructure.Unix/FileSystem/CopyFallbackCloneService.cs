using System.Collections.Concurrent;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// File clone service for Unix/macOS that uses hard links when possible,
/// falling back to standard file copy. CoW (Btrfs/APFS) may be added later.
/// </summary>
public sealed class CopyFallbackCloneService : IFileCloneService
{
    private readonly IHardLinkHandler _hardLinkHandler;
    private readonly ILogger<CopyFallbackCloneService> _logger;
    private readonly ConcurrentDictionary<string, bool> _hardLinkCapable = new(StringComparer.OrdinalIgnoreCase);

    public CopyFallbackCloneService(
        IHardLinkHandler hardLinkHandler,
        ILogger<CopyFallbackCloneService>? logger = null)
    {
        _hardLinkHandler = hardLinkHandler;
        _logger = logger ?? NullLogger<CopyFallbackCloneService>.Instance;
    }

    /// <inheritdoc />
    public CloneCapability CloneFile(string source, string destination)
    {
        var destDir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        // Try hard link first if source and destination are on the same mount
        var sourceMount = GetMountPoint(source);
        var destMount = GetMountPoint(destination);

        if (string.Equals(sourceMount, destMount, StringComparison.Ordinal))
        {
            // Check cached capability
            if (!_hardLinkCapable.TryGetValue(sourceMount, out var capable))
            {
                capable = true; // assume capable until proven otherwise
            }

            if (capable && _hardLinkHandler.TryCreateHardLink(destination, source))
            {
                _logger.LogDebug("Used hard link for {Source}", source);
                _hardLinkCapable[sourceMount] = true;
                return CloneCapability.HardLink;
            }

            // Hard link failed — cache that this mount doesn't support it
            _hardLinkCapable[sourceMount] = false;
        }

        // Fallback to standard copy
        File.Copy(source, destination, overwrite: true);
        _logger.LogDebug("Used standard copy for {Source}", source);
        return CloneCapability.Copy;
    }

    private static string GetMountPoint(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            if (!File.Exists("/proc/mounts"))
                return "/";

            string bestMatch = "/";
            int bestLength = 1;

            foreach (var line in File.ReadLines("/proc/mounts"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                var mountPoint = parts[1];
                if (fullPath.StartsWith(mountPoint, StringComparison.Ordinal) &&
                    mountPoint.Length > bestLength)
                {
                    bestMatch = mountPoint;
                    bestLength = mountPoint.Length;
                }
            }

            return bestMatch;
        }
        catch
        {
            return "/";
        }
    }
}
