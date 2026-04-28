using System.Collections.ObjectModel;
using System.Diagnostics;
using MediaMatch.Infrastructure.Observability;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog.Events;
using Windows.UI;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog for viewing application logs, traces, and diagnostics.
/// </summary>
public sealed partial class LogViewerDialog : ContentDialog
{
    private readonly ObservableCollection<LogEntryViewModel> _allLogs = [];
    private readonly ObservableCollection<LogEntryViewModel> _filteredLogs = [];
    private readonly ObservableCollection<TraceEntryViewModel> _traces = [];
    private readonly ActivityListener _activityListener;

    public LogViewerDialog()
    {
        InitializeComponent();
        LogListView.ItemsSource = _filteredLogs;
        TraceListView.ItemsSource = _traces;

        // Load existing logs
        LoadLogs();
        LoadTraces();

        // Listen for new trace activities
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ActivityNames.SourceName,
            ActivityStopped = activity =>
            {
                if (DispatcherQueue is null) return;
                DispatcherQueue.TryEnqueue(() =>
                {
                    _traces.Insert(0, TraceEntryViewModel.FromActivity(activity));
                });
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);

        Closed += (_, _) => _activityListener.Dispose();
    }

    private void LoadLogs()
    {
        _allLogs.Clear();
        foreach (var evt in InMemoryLogSink.Instance.GetEvents())
        {
            _allLogs.Add(LogEntryViewModel.FromLogEvent(evt));
        }

        ApplyFilter();
    }

    private void LoadTraces()
    {
        _traces.Clear();
        // Traces are captured live via ActivityListener — no historical buffer
    }

    private void ApplyFilter()
    {
        _filteredLogs.Clear();

        var searchText = SearchBox?.Text?.Trim() ?? string.Empty;
        var levelIndex = LevelFilter?.SelectedIndex ?? 0;

        LogEventLevel? minLevel = levelIndex switch
        {
            1 => LogEventLevel.Verbose,
            2 => LogEventLevel.Debug,
            3 => LogEventLevel.Information,
            4 => LogEventLevel.Warning,
            5 => LogEventLevel.Error,
            6 => LogEventLevel.Fatal,
            _ => null
        };

        foreach (var entry in _allLogs)
        {
            if (minLevel.HasValue && entry.RawLevel != minLevel.Value)
                continue;

            if (!string.IsNullOrEmpty(searchText) &&
                !entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                continue;

            _filteredLogs.Add(entry);
        }

        if (LogCountText is not null)
            LogCountText.Text = $"{_filteredLogs.Count} of {_allLogs.Count} log entries";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        InMemoryLogSink.Instance.Clear();
        _allLogs.Clear();
        _filteredLogs.Clear();
        _traces.Clear();
        if (LogCountText is not null)
            LogCountText.Text = "0 log entries";
    }

    private void RefreshLogs_Click(object sender, RoutedEventArgs e) => LoadLogs();

    private async void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch",
            "logs");

        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        await Windows.System.Launcher.LaunchFolderPathAsync(logDir);
    }

    private void TabSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selected = sender.SelectedItem?.Tag?.ToString();
        if (selected == "traces")
        {
            LogsPanel.Visibility = Visibility.Collapsed;
            TracesPanel.Visibility = Visibility.Visible;
        }
        else
        {
            LogsPanel.Visibility = Visibility.Visible;
            TracesPanel.Visibility = Visibility.Collapsed;
        }
    }
}

/// <summary>View model for a single log entry row.</summary>
public sealed class LogEntryViewModel
{
    public string Timestamp { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public LogEventLevel RawLevel { get; init; }
    public SolidColorBrush LevelBrush { get; init; } = new(Color.FromArgb(255, 128, 128, 128));

    public static LogEntryViewModel FromLogEvent(LogEvent evt) => new()
    {
        Timestamp = evt.Timestamp.ToString("HH:mm:ss"),
        Level = evt.Level switch
        {
            LogEventLevel.Verbose     => "VRB",
            LogEventLevel.Debug       => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning     => "WRN",
            LogEventLevel.Error       => "ERR",
            LogEventLevel.Fatal       => "FTL",
            _                         => "???"
        },
        Message = evt.RenderMessage(),
        RawLevel = evt.Level,
        LevelBrush = evt.Level switch
        {
            LogEventLevel.Verbose     => new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
            LogEventLevel.Debug       => new SolidColorBrush(Color.FromArgb(255, 100, 149, 237)),
            LogEventLevel.Information => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),
            LogEventLevel.Warning     => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),
            LogEventLevel.Error       => new SolidColorBrush(Color.FromArgb(255, 231, 72, 86)),
            LogEventLevel.Fatal       => new SolidColorBrush(Color.FromArgb(255, 196, 43, 28)),
            _                         => new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
        }
    };
}

/// <summary>View model for a single trace/activity row.</summary>
public sealed class TraceEntryViewModel
{
    public string Timestamp { get; init; } = string.Empty;
    public string OperationName { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;

    public static TraceEntryViewModel FromActivity(Activity activity) => new()
    {
        Timestamp = activity.StartTimeUtc.ToLocalTime().ToString("HH:mm:ss"),
        OperationName = activity.OperationName,
        Duration = activity.Duration.TotalMilliseconds < 1000
            ? $"{activity.Duration.TotalMilliseconds:F0}ms"
            : $"{activity.Duration.TotalSeconds:F1}s",
        Tags = string.Join(", ", activity.Tags.Select(t => $"{t.Key}={t.Value}"))
    };
}
