using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.Linux.Dialogs;

/// <summary>
/// ViewModel for the file conflict resolution dialog.
/// Displays source/target file info and captures the user's resolution choice.
/// </summary>
public partial class ConflictDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    private string _sourceSize = string.Empty;

    [ObservableProperty]
    private string _targetSize = string.Empty;

    [ObservableProperty]
    private string _sourceLastModified = string.Empty;

    [ObservableProperty]
    private string _targetLastModified = string.Empty;

    /// <summary>
    /// The user's chosen resolution after the dialog closes.
    /// </summary>
    public ConflictResolution Resolution { get; set; } = ConflictResolution.Skip;

    /// <summary>
    /// Populates ViewModel properties from file system info.
    /// </summary>
    public void LoadFromFiles(string sourcePath, string targetPath)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath;

        var sourceInfo = new FileInfo(sourcePath);
        var targetInfo = new FileInfo(targetPath);

        SourceSize = sourceInfo.Exists ? FormatFileSize(sourceInfo.Length) : "Unknown";
        TargetSize = targetInfo.Exists ? FormatFileSize(targetInfo.Length) : "Unknown";

        SourceLastModified = sourceInfo.Exists
            ? sourceInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
            : "Unknown";
        TargetLastModified = targetInfo.Exists
            ? targetInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
            : "Unknown";
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// The user's chosen resolution for a file conflict.
/// </summary>
public enum ConflictResolution
{
    Overwrite,
    Skip,
    RenameAppendNumber,
    CancelAll
}
