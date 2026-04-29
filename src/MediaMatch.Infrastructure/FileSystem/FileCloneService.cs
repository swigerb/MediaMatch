using System.Collections.Concurrent;
using System.Runtime.Versioning;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.FileSystem;

/// <summary>
/// Implements a fallback chain for file cloning:
/// ReFS CoW → NTFS hard link → standard File.Copy.
/// Detects filesystem capabilities at first use and caches the result per volume.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class FileCloneService : IFileCloneService
{
    private readonly ReFsCloneHandler _reFsHandler;
    private readonly HardLinkHandler _hardLinkHandler;
    private readonly ILogger<FileCloneService> _logger;
    private readonly ConcurrentDictionary<string, CloneCapability> _volumeCapabilities = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCloneService"/> class.
    /// </summary>
    /// <param name="reFsHandler">Handler for ReFS Copy-on-Write cloning.</param>
    /// <param name="hardLinkHandler">Handler for NTFS hard link creation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public FileCloneService(
        ReFsCloneHandler reFsHandler,
        HardLinkHandler hardLinkHandler,
        ILogger<FileCloneService>? logger = null)
    {
        _reFsHandler = reFsHandler;
        _hardLinkHandler = hardLinkHandler;
        _logger = logger ?? NullLogger<FileCloneService>.Instance;
    }

    /// <summary>
    /// Clones a file using the best available method for the volume.
    /// Returns the <see cref="CloneCapability"/> that was used.
    /// </summary>
    /// <param name="source">The source file path to clone.</param>
    /// <param name="destination">The destination file path.</param>
    /// <returns>The <see cref="CloneCapability"/> that was actually used for the clone.</returns>
    public CloneCapability CloneFile(string source, string destination)
    {
        var volumeRoot = Path.GetPathRoot(source) ?? string.Empty;

        if (!_volumeCapabilities.TryGetValue(volumeRoot, out var capability))
        {
            capability = DetectCapability(volumeRoot, source, destination);
            _volumeCapabilities.TryAdd(volumeRoot, capability);
        }

        return ExecuteClone(source, destination, capability);
    }

    private CloneCapability DetectCapability(string volumeRoot, string source, string destination)
    {
        // Check if source and destination are on the same volume
        var destRoot = Path.GetPathRoot(destination) ?? string.Empty;
        bool sameVolume = string.Equals(volumeRoot, destRoot, StringComparison.OrdinalIgnoreCase);

        if (_reFsHandler.IsReFs(source))
        {
            _logger.LogInformation("Volume {Volume} detected as ReFS — CoW cloning available", volumeRoot);
            return CloneCapability.CoW;
        }

        if (sameVolume)
        {
            _logger.LogInformation("Volume {Volume} supports hard links (NTFS)", volumeRoot);
            return CloneCapability.HardLink;
        }

        _logger.LogInformation("Cross-volume or unsupported FS — falling back to copy");
        return CloneCapability.Copy;
    }

    private CloneCapability ExecuteClone(string source, string destination, CloneCapability preferredCapability)
    {
        // Ensure destination directory exists
        var destDir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        switch (preferredCapability)
        {
            case CloneCapability.CoW:
                if (_reFsHandler.TryClone(source, destination))
                {
                    _logger.LogDebug("Used ReFS CoW for {Source}", source);
                    return CloneCapability.CoW;
                }
                // Fall through to hard link
                goto case CloneCapability.HardLink;

            case CloneCapability.HardLink:
                if (_hardLinkHandler.TryCreateHardLink(destination, source))
                {
                    _logger.LogDebug("Used hard link for {Source}", source);
                    return CloneCapability.HardLink;
                }
                // Fall through to copy
                goto case CloneCapability.Copy;

            case CloneCapability.Copy:
            default:
                File.Copy(source, destination, overwrite: true);
                _logger.LogDebug("Used standard copy for {Source}", source);
                return CloneCapability.Copy;
        }
    }
}
