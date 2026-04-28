using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Represents a single file in the rename queue with original/new name and match metadata.
/// </summary>
public partial class FileItemViewModel : ViewModelBase
{
    /// <summary>Gets or sets the original file name before renaming.</summary>
    [ObservableProperty]
    public partial string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the new file name after matching/renaming.</summary>
    [ObservableProperty]
    public partial string NewFileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the match confidence score (0.0–1.0).</summary>
    [ObservableProperty]
    public partial double MatchConfidence { get; set; }

    /// <summary>Gets or sets a value indicating whether this file is selected in the UI.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Gets or sets the detected media type (e.g., Video, Subtitle).</summary>
    [ObservableProperty]
    public partial string MediaType { get; set; } = string.Empty;

    /// <summary>Gets or sets the full file path on disk.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the file extension without the leading dot.</summary>
    [ObservableProperty]
    public partial string FileExtension { get; set; } = string.Empty;

    /// <summary>Gets or sets the original folder path.</summary>
    [ObservableProperty]
    public partial string OriginalFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the target folder path for the renamed file.</summary>
    [ObservableProperty]
    public partial string NewFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the metadata provider that produced the match.</summary>
    [ObservableProperty]
    public partial string ProviderSource { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this file has been successfully matched.</summary>
    [ObservableProperty]
    public partial bool IsMatched { get; set; }

    /// <summary>
    /// Screen reader name: filename + status for accessibility.
    /// </summary>
    public string AutomationName => string.IsNullOrEmpty(NewFileName) || NewFileName == OriginalFileName
        ? $"{OriginalFileName}, {MediaType}, no match"
        : $"{OriginalFileName} → {NewFileName}, {MediaType}, {MatchConfidence:P0} confidence";

    /// <summary>
    /// Screen reader name for the selection checkbox.
    /// </summary>
    public string SelectionAutomationName => $"Select {OriginalFileName}";
}
