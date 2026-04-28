using MediaMatch.Core.Models;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Represents a grouped session of rename operations that occurred within a short time window.
/// </summary>
public sealed class HistorySessionViewModel
{
    /// <summary>Gets the timestamp of the earliest entry in this session.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the undo entries belonging to this session.</summary>
    public IReadOnlyList<UndoEntry> Entries { get; }

    /// <summary>Gets the number of files in this session.</summary>
    public int FileCount => Entries.Count;

    /// <summary>Gets a human-readable summary of the session.</summary>
    public string Summary => $"{FileCount} file{(FileCount == 1 ? "" : "s")} renamed";

    /// <summary>Gets the most common media type in this session.</summary>
    public string MediaType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HistorySessionViewModel"/> class.
    /// </summary>
    /// <param name="entries">The undo entries that belong to this session.</param>
    public HistorySessionViewModel(IReadOnlyList<UndoEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0) throw new ArgumentException("Session must contain at least one entry.", nameof(entries));

        Entries = entries;
        Timestamp = entries.Min(e => e.Timestamp);

        // Most common media type in the session
        MediaType = entries
            .GroupBy(e => e.MediaType)
            .OrderByDescending(g => g.Count())
            .First()
            .Key
            .ToString();
    }

    /// <summary>Gets the session timestamp formatted for display.</summary>
    public string FormattedTimestamp => Timestamp.LocalDateTime.ToString("g");
}
