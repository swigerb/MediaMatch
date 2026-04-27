using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Maintains a journal of rename operations and supports undoing them.
/// </summary>
public interface IUndoService
{
    /// <summary>
    /// Record rename operations for future undo.
    /// </summary>
    Task RecordAsync(IReadOnlyList<UndoEntry> entries);

    /// <summary>
    /// Undo the last <paramref name="count"/> operations.
    /// </summary>
    /// <returns>Number of operations actually reversed.</returns>
    Task<int> UndoAsync(int count = 1, CancellationToken ct = default);

    /// <summary>
    /// Check if there are any operations that can be undone.
    /// </summary>
    Task<bool> CanUndoAsync();

    /// <summary>
    /// Get the current undo journal entries (most recent first).
    /// </summary>
    Task<IReadOnlyList<UndoEntry>> GetJournalAsync();
}
