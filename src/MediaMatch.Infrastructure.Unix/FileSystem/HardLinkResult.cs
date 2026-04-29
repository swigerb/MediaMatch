namespace MediaMatch.Infrastructure.Unix.FileSystem;

/// <summary>
/// Outcome of a hard-link attempt, distinguishing filesystem-level limitations
/// from path-specific failures so caching decisions can be made correctly.
/// </summary>
public enum HardLinkResult
{
    /// <summary>The hard link was created successfully.</summary>
    Success,

    /// <summary>
    /// The filesystem (or cross-device situation) does not support hard links.
    /// Safe to cache "no hard links" for the entire mount point.
    /// (e.g. EXDEV, ENOSYS, EOPNOTSUPP / ENOTSUP)
    /// </summary>
    FilesystemUnsupported,

    /// <summary>
    /// Hard link failed for a path-specific reason (permission, target missing,
    /// destination already exists, link count exhausted, etc.). Do not cache —
    /// other paths on the same mount may still succeed.
    /// </summary>
    PathSpecificFailure,
}
