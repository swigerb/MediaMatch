using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Records a single rename operation for undo support.
/// </summary>
/// <param name="OriginalPath">The file path before the rename.</param>
/// <param name="NewPath">The file path after the rename.</param>
/// <param name="Timestamp">The time when the rename was performed.</param>
/// <param name="MediaType">The media type of the renamed file.</param>
public sealed record UndoEntry(
    string OriginalPath,
    string NewPath,
    DateTimeOffset Timestamp,
    MediaType MediaType);
