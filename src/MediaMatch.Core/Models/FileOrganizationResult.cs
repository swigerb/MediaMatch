using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Represents the result of a file organization operation — maps an original path to a new path
/// with confidence and diagnostic information.
/// </summary>
/// <param name="OriginalPath">The original file path before organization.</param>
/// <param name="NewPath">The destination file path, or <c>null</c> if organization failed.</param>
/// <param name="MatchConfidence">The metadata match confidence score between 0 and 1.</param>
/// <param name="MediaType">The detected media type of the file.</param>
/// <param name="Warnings">The list of diagnostic warnings generated during organization.</param>
/// <param name="Success">A value indicating whether the organization succeeded.</param>
public sealed record FileOrganizationResult(
    string OriginalPath,
    string? NewPath,
    float MatchConfidence,
    MediaType MediaType,
    IReadOnlyList<string> Warnings,
    bool Success)
{
    /// <summary>Creates a failed <see cref="FileOrganizationResult"/> with the specified reason.</summary>
    /// <param name="originalPath">The original file path.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A failed <see cref="FileOrganizationResult"/>.</returns>
    public static FileOrganizationResult Failed(string originalPath, string reason) =>
        new(originalPath, null, 0f, MediaType.Unknown, [reason], Success: false);
}
