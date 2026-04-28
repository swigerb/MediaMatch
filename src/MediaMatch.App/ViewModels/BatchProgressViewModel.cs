using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Tracks progress of a batch rename operation for UI binding.
/// </summary>
public partial class BatchProgressViewModel : ViewModelBase
{
    /// <summary>Gets or sets the total number of files in the batch.</summary>
    [ObservableProperty]
    public partial int TotalFiles { get; set; }

    /// <summary>Gets or sets the number of successfully completed files.</summary>
    [ObservableProperty]
    public partial int CompletedFiles { get; set; }

    /// <summary>Gets or sets the number of files that failed processing.</summary>
    [ObservableProperty]
    public partial int FailedFiles { get; set; }

    /// <summary>Gets or sets the name of the file currently being processed.</summary>
    [ObservableProperty]
    public partial string CurrentFile { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the batch operation is running.</summary>
    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Progress percentage (0–100).
    /// </summary>
    public double ProgressPercent => TotalFiles > 0
        ? (double)(CompletedFiles + FailedFiles) / TotalFiles * 100
        : 0;

    /// <summary>Gets a formatted progress string (e.g., "3/10 (1 failed)").</summary>
    public string ProgressText => TotalFiles > 0
        ? $"{CompletedFiles + FailedFiles}/{TotalFiles} ({FailedFiles} failed)"
        : string.Empty;

    /// <summary>
    /// Updates progress counters and raises property-change notifications.
    /// </summary>
    /// <param name="total">Total number of files.</param>
    /// <param name="completed">Number of completed files.</param>
    /// <param name="failed">Number of failed files.</param>
    /// <param name="currentFile">The file currently being processed.</param>
    public void Update(int total, int completed, int failed, string? currentFile)
    {
        TotalFiles = total;
        CompletedFiles = completed;
        FailedFiles = failed;
        CurrentFile = currentFile ?? string.Empty;
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
    }

    /// <summary>
    /// Resets all progress counters to their initial state.
    /// </summary>
    public void Reset()
    {
        TotalFiles = 0;
        CompletedFiles = 0;
        FailedFiles = 0;
        CurrentFile = string.Empty;
        IsRunning = false;
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
    }
}
