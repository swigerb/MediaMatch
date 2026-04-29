using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Services;

/// <summary>
/// Clones files using the best available method for the platform and volume:
/// CoW (ReFS/Btrfs/APFS) → hard link → standard copy.
/// </summary>
public interface IFileCloneService
{
    /// <summary>
    /// Clones a file using the best available method.
    /// </summary>
    /// <param name="source">The source file path to clone.</param>
    /// <param name="destination">The destination file path.</param>
    /// <returns>The <see cref="CloneCapability"/> that was actually used.</returns>
    CloneCapability CloneFile(string source, string destination);
}
