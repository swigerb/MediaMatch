using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the SFV checksum verification panel.
/// </summary>
public partial class SfvPanelViewModel : ViewModelBase
{
    private readonly IChecksumService? _checksumService;
    private readonly ILogger<SfvPanelViewModel> _logger;
    private CancellationTokenSource? _cts;

    /// <summary>Gets the collection of files for checksum verification.</summary>
    public ObservableCollection<SfvFileItemViewModel> Files { get; } = [];

    /// <summary>Gets or sets the selected hash algorithm index.</summary>
    [ObservableProperty]
    public partial int SelectedAlgorithmIndex { get; set; }

    /// <summary>Gets or sets a value indicating whether verification is in progress.</summary>
    [ObservableProperty]
    public partial bool IsVerifying { get; set; }

    /// <summary>Gets or sets the overall verification progress percentage (0–100).</summary>
    [ObservableProperty]
    public partial double TotalProgress { get; set; }

    /// <summary>Gets or sets the total progress text (e.g., "3 / 10").</summary>
    [ObservableProperty]
    public partial string TotalProgressText { get; set; } = string.Empty;

    /// <summary>Gets the available hash algorithm labels.</summary>
    public string[] AlgorithmOptions { get; } = ["SFV", "MD5", "SHA1", "SHA2", "SHA3"];

    /// <summary>Gets a value indicating whether any files are loaded.</summary>
    public bool HasFiles => Files.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SfvPanelViewModel"/> class for design-time use.
    /// </summary>
    public SfvPanelViewModel() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SfvPanelViewModel"/> class.
    /// </summary>
    /// <param name="checksumService">The checksum computation service.</param>
    /// <param name="logger">The logger instance.</param>
    public SfvPanelViewModel(IChecksumService? checksumService, ILogger<SfvPanelViewModel>? logger)
    {
        _checksumService = checksumService;
        _logger = logger ?? NullLogger<SfvPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(ShowEmptyState));
        };
    }

    /// <summary>Gets the visibility of the empty state message.</summary>
    public Microsoft.UI.Xaml.Visibility ShowEmptyState =>
        Files.Count == 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>
    /// Adds files from the specified paths to the verification list.
    /// </summary>
    /// <param name="filePaths">The file paths to add.</param>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var fi = new FileInfo(path);
            if (!fi.Exists) continue;

            Files.Add(new SfvFileItemViewModel
            {
                FileName = fi.Name,
                FilePath = fi.FullName,
                State = SfvState.Pending
            });
        }
    }

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

    [RelayCommand]
    private async Task VerifyAllAsync()
    {
        if (_checksumService is null || Files.Count == 0) return;

        IsVerifying = true;
        _cts = new CancellationTokenSource();
        var algorithm = MapAlgorithm(SelectedAlgorithmIndex);
        var completed = 0;

        try
        {
            foreach (var file in Files)
            {
                if (_cts.Token.IsCancellationRequested) break;

                file.State = SfvState.InProgress;
                file.Progress = 0;

                var progress = new Progress<double>(p =>
                {
                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        file.Progress = p * 100;
                    });
                });

                try
                {
                    var hash = await _checksumService.ComputeAsync(file.FilePath, algorithm, progress, _cts.Token);
                    file.HashValue = hash.ToUpperInvariant();
                    file.State = SfvState.Verified;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Checksum failed for {File}", file.FileName);
                    file.State = SfvState.Failed;
                    file.HashValue = "ERROR";
                }

                completed++;
                TotalProgress = (double)completed / Files.Count * 100;
                TotalProgressText = $"{completed} / {Files.Count}";
            }
        }
        finally
        {
            IsVerifying = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _cts?.Cancel();
        Files.Clear();
        TotalProgress = 0;
        TotalProgressText = string.Empty;
    }

    private static ChecksumAlgorithm MapAlgorithm(int index) => index switch
    {
        0 => ChecksumAlgorithm.Crc32,  // SFV
        1 => ChecksumAlgorithm.Md5,
        2 => ChecksumAlgorithm.Sha1,
        3 => ChecksumAlgorithm.Sha256, // SHA2
        4 => ChecksumAlgorithm.Sha512, // SHA3
        _ => ChecksumAlgorithm.Crc32
    };
}

/// <summary>
/// Represents a single file in the SFV verification list.
/// </summary>
public partial class SfvFileItemViewModel : ViewModelBase
{
    /// <summary>Gets or sets the file name.</summary>
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full file path.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the verification state of this file.</summary>
    [ObservableProperty]
    public partial SfvState State { get; set; } = SfvState.Pending;

    /// <summary>Gets or sets the computed hash value.</summary>
    [ObservableProperty]
    public partial string HashValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the per-file verification progress percentage (0–100).</summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>Gets a display icon representing the current verification state.</summary>
    public string StateIcon => State switch
    {
        SfvState.Verified => "✓",
        SfvState.Failed => "✗",
        SfvState.InProgress => "…",
        _ => "?"
    };
}

/// <summary>
/// Represents the verification state of a file in the SFV panel.
/// </summary>
public enum SfvState
{
    /// <summary>Verification has not started.</summary>
    Pending,

    /// <summary>Verification is currently running.</summary>
    InProgress,

    /// <summary>Verification completed successfully.</summary>
    Verified,

    /// <summary>Verification failed.</summary>
    Failed
}
