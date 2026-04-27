namespace MediaMatch.Core.Enums;

/// <summary>
/// File clone capability detected for a volume.
/// </summary>
public enum CloneCapability
{
    /// <summary>ReFS Copy-on-Write (zero-copy, instant).</summary>
    CoW,

    /// <summary>NTFS hard link (same volume only).</summary>
    HardLink,

    /// <summary>Standard file copy (always available).</summary>
    Copy
}
