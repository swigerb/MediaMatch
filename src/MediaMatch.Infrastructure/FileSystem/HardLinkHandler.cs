using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.FileSystem;

/// <summary>
/// Creates NTFS hard links via the Win32 CreateHardLink API.
/// Works on NTFS volumes only and cannot cross volume boundaries.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class HardLinkHandler
{
    private readonly ILogger<HardLinkHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardLinkHandler"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public HardLinkHandler(ILogger<HardLinkHandler>? logger = null)
    {
        _logger = logger ?? NullLogger<HardLinkHandler>.Instance;
    }

    /// <summary>
    /// Creates a hard link. Returns true on success.
    /// </summary>
    /// <param name="linkPath">The path of the hard link to create.</param>
    /// <param name="targetPath">The existing file to link to.</param>
    /// <returns><see langword="true"/> if the hard link was created successfully; otherwise, <see langword="false"/>.</returns>
    public bool TryCreateHardLink(string linkPath, string targetPath)
    {
        try
        {
            bool result = NativeMethods.CreateHardLink(linkPath, targetPath, IntPtr.Zero);
            if (result)
            {
                _logger.LogInformation("Hard link created: {Link} → {Target}", linkPath, targetPath);
                return true;
            }

            var error = Marshal.GetLastWin32Error();
            _logger.LogDebug("CreateHardLink failed (error {Error}) for {Target}", error, targetPath);
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
        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes);
    }
}
