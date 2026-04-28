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
    /// <param name="entries">The undo entries to record.</param>
    Task RecordAsync(IReadOnlyList<UndoEntry> entries);

    /// <summary>
    /// Undo the last <paramref name="count"/> operations.
    /// </summary>
    /// <param name="count">The number of operations to undo.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The number of operations actually reversed.</returns>
    Task<int> UndoAsync(int count = 1, CancellationToken ct = default);

    /// <summary>
    /// Check if there are any operations that can be undone.
    /// </summary>
    /// <returns>A value indicating whether undo is possible.</returns>
    Task<bool> CanUndoAsync();

    /// <summary>
    /// Get the current undo journal entries (most recent first).
    /// </summary>
    /// <returns>A read-only list of undo journal entries.</returns>
    Task<IReadOnlyList<UndoEntry>> GetJournalAsync();
}
