namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a batch rename operation with progress tracking.
/// </summary>
public sealed class BatchJob
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public IReadOnlyList<BatchFileItem> Files { get; init; } = [];
    public BatchStatus Status { get; set; } = BatchStatus.Pending;
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public string? CurrentFile { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// A single file within a batch operation.
/// </summary>
public sealed class BatchFileItem
{
    public required string FilePath { get; init; }
    public string? NewPath { get; set; }
    public BatchFileStatus Status { get; set; } = BatchFileStatus.Pending;
    public string? Error { get; set; }
}

public enum BatchStatus
{
    Pending,
    Running,
    Completed,
    Cancelled,
    Failed
}

public enum BatchFileStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Skipped
}
