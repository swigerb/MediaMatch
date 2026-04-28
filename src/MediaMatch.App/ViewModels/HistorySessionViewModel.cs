using MediaMatch.Core.Models;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Represents a grouped session of rename operations that occurred within a short time window.
/// </summary>
public sealed class HistorySessionViewModel
{
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyList<UndoEntry> Entries { get; }
    public int FileCount => Entries.Count;
    public string Summary => $"{FileCount} file{(FileCount == 1 ? "" : "s")} renamed";
    public string MediaType { get; }

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

    public string FormattedTimestamp => Timestamp.LocalDateTime.ToString("g");
}
