using MediaMatch.Core.Services;

namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Unix-specific extension of <see cref="IHardLinkHandler"/> that surfaces the reason a
/// hard-link attempt failed so callers can distinguish filesystem-level limitations from
/// path-specific failures.
/// </summary>
public interface IUnixHardLinkHandler : IHardLinkHandler
{
    /// <summary>
    /// Attempts to create a hard link and reports the classified failure reason on error.
    /// </summary>
    HardLinkResult TryCreateHardLinkWithResult(string linkPath, string targetPath);
}
