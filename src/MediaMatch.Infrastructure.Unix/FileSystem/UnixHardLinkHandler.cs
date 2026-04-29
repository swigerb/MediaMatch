using System.Runtime.InteropServices;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Creates hard links on Unix/macOS using the POSIX link() syscall via P/Invoke.
/// Falls back gracefully if the file system does not support hard links.
/// </summary>
public sealed partial class UnixHardLinkHandler : IUnixHardLinkHandler
{
    // Linux errno values
    private const int LinuxENOSYS = 38;
    private const int LinuxEOPNOTSUPP = 95;

    // macOS errno values
    private const int MacENOSYS = 78;
    private const int MacEOPNOTSUPP = 102;

    // Shared
    private const int EXDEV = 18;

    private readonly ILogger<UnixHardLinkHandler> _logger;

    public UnixHardLinkHandler(ILogger<UnixHardLinkHandler>? logger = null)
    {
        _logger = logger ?? NullLogger<UnixHardLinkHandler>.Instance;
    }

    /// <inheritdoc />
    public bool TryCreateHardLink(string linkPath, string targetPath)
        => TryCreateHardLinkWithResult(linkPath, targetPath) == HardLinkResult.Success;

    /// <inheritdoc />
    public HardLinkResult TryCreateHardLinkWithResult(string linkPath, string targetPath)
    {
        try
        {
            if (!File.Exists(targetPath))
            {
                _logger.LogDebug("Target file does not exist: {Target}", targetPath);
                return HardLinkResult.PathSpecificFailure;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _logger.LogDebug("Hard links not supported on this platform");
                return HardLinkResult.FilesystemUnsupported;
            }

            int result = NativeMethods.link(targetPath, linkPath);
            if (result == 0)
            {
                _logger.LogInformation("Hard link created: {Link} → {Target}", linkPath, targetPath);
                return HardLinkResult.Success;
            }

            int errno = Marshal.GetLastPInvokeError();
            var classification = ClassifyErrno(errno);
            _logger.LogDebug(
                "link() failed (errno {Errno}, {Classification}) for {Target}",
                errno, classification, targetPath);
            return classification;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Hard link creation failed for {Target}", targetPath);
            return HardLinkResult.PathSpecificFailure;
        }
    }

    private static HardLinkResult ClassifyErrno(int errno)
    {
        if (errno == EXDEV)
            return HardLinkResult.FilesystemUnsupported;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (errno == MacENOSYS || errno == MacEOPNOTSUPP)
                return HardLinkResult.FilesystemUnsupported;
        }
        else
        {
            if (errno == LinuxENOSYS || errno == LinuxEOPNOTSUPP)
                return HardLinkResult.FilesystemUnsupported;
        }

        // EEXIST, EACCES, EPERM, EMLINK, ENOENT, ENOTDIR, etc. — path-specific
        return HardLinkResult.PathSpecificFailure;
    }

    private static partial class NativeMethods
    {
        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int link(string oldpath, string newpath);
    }
}
