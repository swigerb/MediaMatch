using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Represents a single file in the rename queue with original/new name and match metadata.
/// </summary>
public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string OriginalFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double MatchConfidence { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string MediaType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileExtension { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginalFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProviderSource { get; set; } = string.Empty;

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
