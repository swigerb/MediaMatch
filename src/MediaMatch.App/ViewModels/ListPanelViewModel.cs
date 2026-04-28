using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if WINDOWS
using Windows.Storage.Pickers;
#endif

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the List export panel — pattern-based file listing with sequence numbering.
/// </summary>
public partial class ListPanelViewModel : ViewModelBase
{
    private readonly IExpressionEngine? _expressionEngine;
    private readonly ILogger<ListPanelViewModel> _logger;

    /// <summary>Gets the collection of files for pattern-based listing.</summary>
    public ObservableCollection<ListFileItemViewModel> Files { get; } = [];

    /// <summary>Gets or sets the rename pattern template.</summary>
    [ObservableProperty]
    public partial string Pattern { get; set; } = "Sequence - {i.pad(2)}";

    /// <summary>Gets or sets the starting sequence number.</summary>
    [ObservableProperty]
    public partial int FromNumber { get; set; } = 1;

    /// <summary>Gets or sets the ending sequence number.</summary>
    [ObservableProperty]
    public partial int ToNumber { get; set; } = 20;

    /// <summary>Gets or sets the status message displayed in the panel.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether any files are loaded.</summary>
    public bool HasFiles => Files.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPanelViewModel"/> class for design-time use.
    /// </summary>
    public ListPanelViewModel() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPanelViewModel"/> class.
    /// </summary>
    /// <param name="expressionEngine">The expression evaluation engine.</param>
    /// <param name="logger">The logger instance.</param>
    public ListPanelViewModel(IExpressionEngine? expressionEngine, ILogger<ListPanelViewModel>? logger)
    {
        _expressionEngine = expressionEngine;
        _logger = logger ?? NullLogger<ListPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    /// <summary>
    /// Adds files from the specified paths to the panel.
    /// </summary>
    /// <param name="filePaths">The file paths to add.</param>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var fi = new FileInfo(path);
            if (!fi.Exists) continue;

            Files.Add(new ListFileItemViewModel
            {
                FileName = fi.Name,
                FilePath = fi.FullName,
                FolderName = fi.Directory?.Name ?? string.Empty,
                Output = fi.Name
            });
        }
    }

    [RelayCommand]
    private void ApplyPattern()
    {
        if (_expressionEngine is null || Files.Count == 0) return;

        var seq = FromNumber;
        foreach (var file in Files)
        {
            try
            {
                var output = Pattern
                    .Replace("{fn}", Path.GetFileNameWithoutExtension(file.FilePath))
                    .Replace("{ext}", Path.GetExtension(file.FilePath))
                    .Replace("{i}", seq.ToString())
                    .Replace("{folder}", file.FolderName);
                file.Output = output;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pattern error for {File}", file.FileName);
                file.Output = file.FileName;
            }
            seq++;
        }

        StatusMessage = $"Applied pattern to {Files.Count} file(s).";
    }

    [RelayCommand]
    private void Clear()
    {
        Files.Clear();
        StatusMessage = string.Empty;
    }

#if WINDOWS
    [RelayCommand]
    private async Task LoadFilesAsync()
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0) return;

        AddFiles(files.Select(f => f.Path));
    }
#endif
}

/// <summary>
/// A file item in the List panel with evaluated output text.
/// </summary>
public partial class ListFileItemViewModel : ViewModelBase
{
    /// <summary>Gets or sets the file name.</summary>
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full file path.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent folder name.</summary>
    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the evaluated output text after pattern application.</summary>
    [ObservableProperty]
    public partial string Output { get; set; } = string.Empty;
}
