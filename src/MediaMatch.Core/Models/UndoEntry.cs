using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Records a single rename operation for undo support.
/// </summary>
public sealed record UndoEntry(
    string OriginalPath,
    string NewPath,
    DateTimeOffset Timestamp,
    MediaType MediaType);
