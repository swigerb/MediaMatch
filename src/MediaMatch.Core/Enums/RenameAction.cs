namespace MediaMatch.Core.Enums;

/// <summary>
/// Specifies the file operation to perform when renaming or organizing media files.
/// </summary>
public enum RenameAction
{
    /// <summary>Moves the file to the destination, removing the original.</summary>
    Move,

    /// <summary>Copies the file to the destination, preserving the original.</summary>
    Copy,

    /// <summary>Creates an NTFS hard link at the destination (same volume only).</summary>
    Hardlink,

    /// <summary>Creates a symbolic link at the destination.</summary>
    Symlink,

    /// <summary>Creates a ReFS reflink (copy-on-write) clone at the destination.</summary>
    Reflink,

    /// <summary>Simulates the operation without modifying any files.</summary>
    Test,

    /// <summary>Automatically selects the best available clone method for the volume.</summary>
    Clone
}
