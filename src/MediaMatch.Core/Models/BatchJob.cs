namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a batch rename operation with progress tracking.
/// </summary>
public sealed class BatchJob
{
    /// <summary>Gets the unique identifier for this batch job.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Gets the list of files included in this batch operation.</summary>
    public IReadOnlyList<BatchFileItem> Files { get; init; } = [];

    /// <summary>Gets or sets the current status of the batch operation.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    /// <summary>Gets or sets the number of files successfully processed.</summary>
    public int CompletedCount { get; set; }

    /// <summary>Gets or sets the number of files that failed to process.</summary>
    public int FailedCount { get; set; }

    /// <summary>Gets or sets the path of the file currently being processed.</summary>
    public string? CurrentFile { get; set; }

    /// <summary>Gets or sets the timestamp when the batch operation started.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Gets or sets the timestamp when the batch operation completed, or <c>null</c> if still running.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Represents a single file within a batch operation.
/// </summary>
public sealed class BatchFileItem
{
    /// <summary>Gets the original file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Gets or sets the destination path after renaming.</summary>
    public string? NewPath { get; set; }

    /// <summary>Gets or sets the processing status of this file.</summary>
    public BatchFileStatus Status { get; set; } = BatchFileStatus.Pending;

    /// <summary>Gets or sets the error message if processing failed.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Specifies the overall status of a <see cref="BatchJob"/>.
/// </summary>
public enum BatchStatus
{
    /// <summary>The batch is queued and has not started.</summary>
    Pending,

    /// <summary>The batch is currently processing files.</summary>
    Running,

    /// <summary>The batch finished processing all files.</summary>
    Completed,

    /// <summary>The batch was cancelled by the user.</summary>
    Cancelled,

    /// <summary>The batch terminated due to an unrecoverable error.</summary>
    Failed
}

/// <summary>
/// Specifies the processing status of a single <see cref="BatchFileItem"/>.
/// </summary>
public enum BatchFileStatus
{
    /// <summary>The file is queued and has not been processed.</summary>
    Pending,

    /// <summary>The file is currently being processed.</summary>
    Processing,

    /// <summary>The file was processed successfully.</summary>
    Success,

    /// <summary>The file failed to process.</summary>
    Failed,

    /// <summary>The file was skipped (e.g., already at the target location).</summary>
    Skipped
}
