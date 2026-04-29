using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.macOS.ViewModels;

/// <summary>
/// ViewModel for the MediaInfo Inspector dialog.
/// Loads ffprobe data for a file and exposes per-stream property collections.
/// </summary>
public partial class MediaInfoInspectorViewModel : ViewModelBase
{
    private readonly IMediaInfoService _mediaInfoService;
    private readonly ILogger<MediaInfoInspectorViewModel> _logger;
    private MediaInfoResult? _result;

    /// <summary>Gets or sets the path of the file being inspected.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether media info is being loaded.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether ffprobe is available on the system.</summary>
    [ObservableProperty]
    public partial bool IsFfprobeAvailable { get; set; } = true;

    /// <summary>Gets or sets the error message to display when loading fails.</summary>
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Gets the general container properties for the loaded file.</summary>
    public ObservableCollection<PropertyItem> GeneralProperties { get; } = [];

    /// <summary>Gets the video stream groups for the loaded file.</summary>
    public ObservableCollection<StreamGroup> VideoStreams { get; } = [];

    /// <summary>Gets the audio stream groups for the loaded file.</summary>
    public ObservableCollection<StreamGroup> AudioStreams { get; } = [];

    /// <summary>Gets the text/subtitle stream groups for the loaded file.</summary>
    public ObservableCollection<StreamGroup> TextStreams { get; } = [];

    /// <summary>Gets a value indicating whether a media info result has been loaded.</summary>
    public bool HasResult => _result is not null;

    public MediaInfoInspectorViewModel() : this(null!, null) { }

    public MediaInfoInspectorViewModel(IMediaInfoService mediaInfoService, ILogger<MediaInfoInspectorViewModel>? logger)
    {
        _mediaInfoService = mediaInfoService;
        _logger = logger ?? NullLogger<MediaInfoInspectorViewModel>.Instance;
    }

    [RelayCommand]
    private async Task LoadFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        FilePath = filePath;
        IsLoading = true;
        ErrorMessage = string.Empty;
        _result = null;
        ClearCollections();

        try
        {
            var available = await _mediaInfoService.IsAvailableAsync();
            IsFfprobeAvailable = available;

            if (!available)
            {
                ErrorMessage = "ffprobe was not found. Install FFmpeg and ensure ffprobe is on your system PATH.\n\nDownload: https://ffmpeg.org/download.html";
                return;
            }

            var result = await _mediaInfoService.GetMediaInfoAsync(filePath);
            if (result is null)
            {
                ErrorMessage = "Could not read media information from this file.";
                return;
            }

            _result = result;
            PopulateCollections(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load media info for {File}", filePath);
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasResult));
        }
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (_result is null) return;

        var text = _result.ExportAsText();
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    private void ClearCollections()
    {
        GeneralProperties.Clear();
        VideoStreams.Clear();
        AudioStreams.Clear();
        TextStreams.Clear();
    }

    private void PopulateCollections(MediaInfoResult result)
    {
        foreach (var kv in result.General)
            GeneralProperties.Add(new PropertyItem(kv.Key, kv.Value));

        for (var i = 0; i < result.VideoStreams.Count; i++)
        {
            var items = result.VideoStreams[i].Select(kv => new PropertyItem(kv.Key, kv.Value)).ToList();
            VideoStreams.Add(new StreamGroup($"Video #{i + 1}", items));
        }

        for (var i = 0; i < result.AudioStreams.Count; i++)
        {
            var items = result.AudioStreams[i].Select(kv => new PropertyItem(kv.Key, kv.Value)).ToList();
            AudioStreams.Add(new StreamGroup($"Audio #{i + 1}", items));
        }

        for (var i = 0; i < result.TextStreams.Count; i++)
        {
            var items = result.TextStreams[i].Select(kv => new PropertyItem(kv.Key, kv.Value)).ToList();
            TextStreams.Add(new StreamGroup($"Text #{i + 1}", items));
        }
    }
}

/// <summary>
/// A single key-value property for display in the property grid.
/// </summary>
public sealed class PropertyItem(string key, string value)
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}

/// <summary>
/// A named group of properties representing one stream.
/// </summary>
public sealed class StreamGroup(string name, IReadOnlyList<PropertyItem> properties)
{
    public string Name { get; } = name;
    public IReadOnlyList<PropertyItem> Properties { get; } = properties;
}
