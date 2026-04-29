using System.Runtime.InteropServices;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Creates hard links on Unix/macOS using the POSIX link() syscall via .NET APIs.
/// Falls back gracefully if the file system does not support hard links.
/// </summary>
public sealed partial class UnixHardLinkHandler : IHardLinkHandler
{
    private readonly ILogger<UnixHardLinkHandler> _logger;

    public UnixHardLinkHandler(ILogger<UnixHardLinkHandler>? logger = null)
    {
        _logger = logger ?? NullLogger<UnixHardLinkHandler>.Instance;
    }

    /// <inheritdoc />
    public bool TryCreateHardLink(string linkPath, string targetPath)
    {
        try
        {
            if (!File.Exists(targetPath))
            {
                _logger.LogDebug("Target file does not exist: {Target}", targetPath);
                return false;
            }

            // .NET 7+ provides File.CreateHardLink — use it if available at runtime,
            // otherwise fall back to the POSIX link() syscall via P/Invoke.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                int result = NativeMethods.link(targetPath, linkPath);
                if (result == 0)
                {
                    _logger.LogInformation("Hard link created: {Link} → {Target}", linkPath, targetPath);
                    return true;
                }

                int errno = Marshal.GetLastPInvokeError();
                _logger.LogDebug("link() failed (errno {Errno}) for {Target}", errno, targetPath);
                return false;
            }

            _logger.LogDebug("Hard links not supported on this platform");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Hard link creation failed for {Target}", targetPath);
            return false;
        }
    }

    private static partial class NativeMethods
    {
        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int link(string oldpath, string newpath);
    }
}
