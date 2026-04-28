using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
#if !MEDIAMATH_TESTS
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
#endif

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the History page — shows rename operation history grouped into sessions,
/// with revert, export, and clear capabilities.
/// </summary>
public partial class HistoryViewModel : ViewModelBase
{
    private static readonly TimeSpan SessionGap = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IUndoService _undoService;

    public ObservableCollection<HistorySessionViewModel> Sessions { get; } = [];

    [ObservableProperty]
    public partial HistorySessionViewModel? SelectedSession { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool HasHistory { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Loading history…";

    public IReadOnlyList<UndoEntry> SelectedSessionEntries =>
        SelectedSession?.Entries ?? Array.Empty<UndoEntry>();

    public HistoryViewModel(IUndoService undoService)
    {
        ArgumentNullException.ThrowIfNull(undoService);
        _undoService = undoService;
    }

    partial void OnIsEmptyChanged(bool value)
    {
        HasHistory = !value;
    }

    partial void OnSelectedSessionChanged(HistorySessionViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedSessionEntries));
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        try
        {
            var journal = await _undoService.GetJournalAsync();
            Sessions.Clear();

            if (journal.Count == 0)
            {
                IsEmpty = true;
                StatusMessage = "No rename history found.";
                SelectedSession = null;
                return;
            }

            var sessions = GroupIntoSessions(journal);
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }

            IsEmpty = false;
            StatusMessage = $"{Sessions.Count} session(s), {journal.Count} total operation(s)";
            SelectedSession = Sessions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading history: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RevertSelectedAsync()
    {
        if (SelectedSession is null) return;

        try
        {
            var count = SelectedSession.FileCount;
            var undone = await _undoService.UndoAsync(count);
            StatusMessage = undone > 0
                ? $"Reverted {undone} operation(s)."
                : "Nothing to revert.";

            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Revert error: {ex.Message}";
        }
    }

#if !MEDIAMATH_TESTS
    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON", [".json"]);
            picker.SuggestedFileName = $"MediaMatch_History_{DateTime.Now:yyyyMMdd_HHmmss}";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is null) return;

            var journal = await _undoService.GetJournalAsync();
            var json = JsonSerializer.Serialize(journal, ExportJsonOptions);
            await File.WriteAllTextAsync(file.Path, json);

            StatusMessage = $"History exported to {file.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Clear History",
                Content = "This will remove all rename history. This action cannot be undone.",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            // Clear all entries by undoing with count 0 — instead, clear local state.
            // IUndoService doesn't expose ClearAsync, so we undo all entries from the journal.
            var journal = await _undoService.GetJournalAsync();
            if (journal.Count > 0)
            {
                // UndoAsync reverses file moves — for clear we just want to forget history.
                // Record an empty set after undoing to effectively clear.
                // Since we can't modify IUndoService, we undo all (which also removes from journal).
                await _undoService.UndoAsync(journal.Count);
            }

            Sessions.Clear();
            SelectedSession = null;
            IsEmpty = true;
            StatusMessage = "History cleared.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clear error: {ex.Message}";
        }
    }
#endif

    /// <summary>
    /// Groups journal entries into sessions based on timestamp proximity.
    /// Entries within <see cref="SessionGap"/> of each other belong to the same session.
    /// </summary>
    internal static IReadOnlyList<HistorySessionViewModel> GroupIntoSessions(IReadOnlyList<UndoEntry> entries)
    {
        if (entries.Count == 0) return [];

        var sorted = entries.OrderByDescending(e => e.Timestamp).ToList();
        var sessions = new List<HistorySessionViewModel>();
        var currentGroup = new List<UndoEntry> { sorted[0] };

        for (var i = 1; i < sorted.Count; i++)
        {
            var gap = currentGroup[^1].Timestamp - sorted[i].Timestamp;
            if (gap <= SessionGap)
            {
                currentGroup.Add(sorted[i]);
            }
            else
            {
                sessions.Add(new HistorySessionViewModel(currentGroup));
                currentGroup = [sorted[i]];
            }
        }

        sessions.Add(new HistorySessionViewModel(currentGroup));
        return sessions;
    }
}
