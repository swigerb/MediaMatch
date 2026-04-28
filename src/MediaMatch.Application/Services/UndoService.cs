using System.Text.Json;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Maintains a rolling journal of rename operations in %LOCALAPPDATA%/MediaMatch/undo.json.
/// Supports undoing the most recent operations by reversing the file moves.
/// </summary>
public sealed class UndoService : IUndoService
{
    private const int MaxEntries = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UndoService> _logger;
    private readonly string _journalPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction for move operations.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="journalPath">Optional custom path for the undo journal file.</param>
    public UndoService(
        IFileSystem fileSystem,
        ILogger<UndoService>? logger = null,
        string? journalPath = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
        _logger = logger ?? NullLogger<UndoService>.Instance;

        _journalPath = journalPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch",
            "undo.json");
    }

    /// <inheritdoc />
    public async Task RecordAsync(IReadOnlyList<UndoEntry> entries)
    {
        if (entries.Count == 0) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var journal = await LoadJournalAsync().ConfigureAwait(false);
            journal.AddRange(entries);

            // Keep only the most recent MaxEntries
            if (journal.Count > MaxEntries)
            {
                journal = journal.Skip(journal.Count - MaxEntries).ToList();
            }

            await SaveJournalAsync(journal).ConfigureAwait(false);
            _logger.LogInformation("Recorded {Count} undo entries ({Total} total)", entries.Count, journal.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> UndoAsync(int count = 1, CancellationToken ct = default)
    {
        if (count <= 0) return 0;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var journal = await LoadJournalAsync().ConfigureAwait(false);
            if (journal.Count == 0) return 0;

            int toUndo = Math.Min(count, journal.Count);
            int undone = 0;

            // Undo from most recent backwards
            for (int i = journal.Count - 1; i >= journal.Count - toUndo; i--)
            {
                ct.ThrowIfCancellationRequested();

                var entry = journal[i];
                try
                {
                    if (_fileSystem.FileExists(entry.NewPath))
                    {
                        // Ensure original directory exists
                        var dir = Path.GetDirectoryName(entry.OriginalPath);
                        if (!string.IsNullOrEmpty(dir))
                            _fileSystem.CreateDirectory(dir);

                        _fileSystem.MoveFile(entry.NewPath, entry.OriginalPath);
                        undone++;
                        _logger.LogInformation("Undid rename: {New} → {Original}", entry.NewPath, entry.OriginalPath);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot undo: file not found at {Path}", entry.NewPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to undo rename for {Path}", entry.NewPath);
                }
            }

            // Remove undone entries from journal
            journal.RemoveRange(journal.Count - toUndo, toUndo);
            await SaveJournalAsync(journal).ConfigureAwait(false);

            return undone;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanUndoAsync()
    {
        var journal = await LoadJournalAsync().ConfigureAwait(false);
        return journal.Count > 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UndoEntry>> GetJournalAsync()
    {
        var journal = await LoadJournalAsync().ConfigureAwait(false);
        journal.Reverse();
        return journal;
    }

    private async Task<List<UndoEntry>> LoadJournalAsync()
    {
        try
        {
            if (!File.Exists(_journalPath))
                return [];

            var json = await File.ReadAllTextAsync(_journalPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<UndoEntry>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load undo journal, starting fresh");
            return [];
        }
    }

    private async Task SaveJournalAsync(List<UndoEntry> journal)
    {
        var dir = Path.GetDirectoryName(_journalPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(journal, JsonOptions);
        var tempPath = _journalPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
        File.Move(tempPath, _journalPath, overwrite: true);
    }
}
