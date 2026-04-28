using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the SFV checksum verification panel.
/// </summary>
public partial class SfvPanelViewModel : ViewModelBase
{
    private readonly IChecksumService? _checksumService;
    private readonly ILogger<SfvPanelViewModel> _logger;
    private CancellationTokenSource? _cts;

    public ObservableCollection<SfvFileItemViewModel> Files { get; } = [];

    [ObservableProperty]
    public partial int SelectedAlgorithmIndex { get; set; }

    [ObservableProperty]
    public partial bool IsVerifying { get; set; }

    [ObservableProperty]
    public partial double TotalProgress { get; set; }

    [ObservableProperty]
    public partial string TotalProgressText { get; set; } = string.Empty;

    public string[] AlgorithmOptions { get; } = ["SFV", "MD5", "SHA1", "SHA2", "SHA3"];

    public bool HasFiles => Files.Count > 0;

    public SfvPanelViewModel() : this(null, null) { }

    public SfvPanelViewModel(IChecksumService? checksumService, ILogger<SfvPanelViewModel>? logger)
    {
        _checksumService = checksumService;
        _logger = logger ?? NullLogger<SfvPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

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
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SfvState State { get; set; } = SfvState.Pending;

    [ObservableProperty]
    public partial string HashValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Progress { get; set; }

    public string StateIcon => State switch
    {
        SfvState.Verified => "✓",
        SfvState.Failed => "✗",
        SfvState.InProgress => "…",
        _ => "?"
    };
}

public enum SfvState
{
    Pending,
    InProgress,
    Verified,
    Failed
}
