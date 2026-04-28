using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the List export panel — pattern-based file listing with sequence numbering.
/// </summary>
public partial class ListPanelViewModel : ViewModelBase
{
    private readonly IExpressionEngine? _expressionEngine;
    private readonly ILogger<ListPanelViewModel> _logger;

    public ObservableCollection<ListFileItemViewModel> Files { get; } = [];

    [ObservableProperty]
    public partial string Pattern { get; set; } = "{fn}";

    [ObservableProperty]
    public partial int FromNumber { get; set; } = 1;

    [ObservableProperty]
    public partial int ToNumber { get; set; } = 100;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool HasFiles => Files.Count > 0;

    public ListPanelViewModel() : this(null, null) { }

    public ListPanelViewModel(IExpressionEngine? expressionEngine, ILogger<ListPanelViewModel>? logger)
    {
        _expressionEngine = expressionEngine;
        _logger = logger ?? NullLogger<ListPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

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
}

/// <summary>
/// A file item in the List panel with evaluated output text.
/// </summary>
public partial class ListFileItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Output { get; set; } = string.Empty;
}
