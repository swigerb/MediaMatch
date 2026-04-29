namespace MediaMatch.Core.Services;

/// <summary>
/// Creates hard links on the local file system.
/// </summary>
public interface IHardLinkHandler
{
    /// <summary>
    /// Attempts to create a hard link pointing to an existing file.
    /// </summary>
    /// <param name="linkPath">The path of the hard link to create.</param>
    /// <param name="targetPath">The existing file to link to.</param>
    /// <returns><see langword="true"/> if the hard link was created successfully; otherwise, <see langword="false"/>.</returns>
    bool TryCreateHardLink(string linkPath, string targetPath);
}
