using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Tracks progress of a batch rename operation for UI binding.
/// </summary>
public partial class BatchProgressViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int TotalFiles { get; set; }

    [ObservableProperty]
    public partial int CompletedFiles { get; set; }

    [ObservableProperty]
    public partial int FailedFiles { get; set; }

    [ObservableProperty]
    public partial string CurrentFile { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Progress percentage (0–100).
    /// </summary>
    public double ProgressPercent => TotalFiles > 0
        ? (double)(CompletedFiles + FailedFiles) / TotalFiles * 100
        : 0;

    public string ProgressText => TotalFiles > 0
        ? $"{CompletedFiles + FailedFiles}/{TotalFiles} ({FailedFiles} failed)"
        : string.Empty;

    public void Update(int total, int completed, int failed, string? currentFile)
    {
        TotalFiles = total;
        CompletedFiles = completed;
        FailedFiles = failed;
        CurrentFile = currentFile ?? string.Empty;
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
    }

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
