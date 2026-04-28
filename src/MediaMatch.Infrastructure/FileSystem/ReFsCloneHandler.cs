using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32.SafeHandles;

namespace MediaMatch.Infrastructure.FileSystem;

/// <summary>
/// Attempts ReFS Copy-on-Write clone via FSCTL_DUPLICATE_EXTENTS_TO_FILE.
/// Zero-copy, instant operation on ReFS volumes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class ReFsCloneHandler
{
    private readonly ILogger<ReFsCloneHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReFsCloneHandler"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ReFsCloneHandler(ILogger<ReFsCloneHandler>? logger = null)
    {
        _logger = logger ?? NullLogger<ReFsCloneHandler>.Instance;
    }

    /// <summary>
    /// Returns true if the volume at the given path is ReFS.
    /// </summary>
    /// <param name="path">A file or directory path on the volume to check.</param>
    /// <returns><see langword="true"/> if the volume uses the ReFS file system; otherwise, <see langword="false"/>.</returns>
    public bool IsReFs(string path)
    {
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
            return false;

        var fsName = new char[256];
        var volName = new char[256];
        bool success = NativeMethods.GetVolumeInformation(
            root, volName, volName.Length,
            out _, out _, out _,
            fsName, fsName.Length);

        if (!success)
            return false;

        var fileSystem = new string(fsName).TrimEnd('\0');
        return string.Equals(fileSystem, "ReFS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clones a file using ReFS CoW. Returns true on success.
    /// </summary>
    /// <param name="source">The source file path to clone.</param>
    /// <param name="destination">The destination file path.</param>
    /// <returns><see langword="true"/> if the CoW clone succeeded; otherwise, <see langword="false"/>.</returns>
    public bool TryClone(string source, string destination)
    {
        try
        {
            // Copy to create the destination, then deduplicate
            File.Copy(source, destination, overwrite: true);

            using var sourceHandle = File.OpenHandle(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var destHandle = File.OpenHandle(destination, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var fileInfo = new FileInfo(source);
            var duplicateExtents = new NativeMethods.DUPLICATE_EXTENTS_DATA
            {
                FileHandle = sourceHandle.DangerousGetHandle(),
                SourceFileOffset = 0,
                TargetFileOffset = 0,
                ByteCount = fileInfo.Length
            };

            int size = Marshal.SizeOf<NativeMethods.DUPLICATE_EXTENTS_DATA>();
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(duplicateExtents, ptr, false);
                bool result = NativeMethods.DeviceIoControl(
                    destHandle.DangerousGetHandle(),
                    NativeMethods.FSCTL_DUPLICATE_EXTENTS_TO_FILE,
                    ptr, (uint)size,
                    IntPtr.Zero, 0,
                    out _, IntPtr.Zero);

                if (result)
                {
                    _logger.LogInformation("ReFS CoW clone succeeded: {Source} → {Destination}", source, destination);
                    return true;
                }

                _logger.LogDebug("ReFS CoW clone failed (error {Error}), falling back", Marshal.GetLastWin32Error());
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ReFS CoW clone failed for {Source}", source);
            return false;
        }
    }

    private static partial class NativeMethods
    {
        internal const uint FSCTL_DUPLICATE_EXTENTS_TO_FILE = 0x00098344;

        [StructLayout(LayoutKind.Sequential)]
        internal struct DUPLICATE_EXTENTS_DATA
        {
            public IntPtr FileHandle;
            public long SourceFileOffset;
            public long TargetFileOffset;
            public long ByteCount;
        }

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetVolumeInformation(
            string lpRootPathName,
            [Out] char[] lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            [Out] char[] lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);
    }
}
