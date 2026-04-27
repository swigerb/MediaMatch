using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Result of a file organization operation — maps an original path to a new path
/// with confidence and diagnostic information.
/// </summary>
public sealed record FileOrganizationResult(
    string OriginalPath,
    string? NewPath,
    float MatchConfidence,
    MediaType MediaType,
    IReadOnlyList<string> Warnings,
    bool Success)
{
    public static FileOrganizationResult Failed(string originalPath, string reason) =>
        new(originalPath, null, 0f, MediaType.Unknown, [reason], Success: false);
}
