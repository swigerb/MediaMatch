using CommunityToolkit.Mvvm.ComponentModel;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the preset editor dialog — manages all preset properties
/// and converts between UI indices and domain model values.
/// </summary>
public partial class PresetEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IncludeFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RenamePattern { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string KeyboardShortcut { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedDatasourceIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedEpisodeOrderIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedMatchModeIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedRenameActionIndex { get; set; }

    public static string[] DatasourceOptions { get; } = ["Auto", "TMDb", "TVDb", "AniDB", "MusicBrainz"];
    public static string[] LanguageOptions { get; } = ["English", "Japanese", "German", "French", "Spanish", "Korean", "Chinese", "Portuguese", "Italian", "Russian"];
    public static string[] EpisodeOrderOptions { get; } = ["Airdate", "DVD", "Absolute"];
    public static string[] MatchModeOptions { get; } = ["Opportunistic", "Strict"];
    public static string[] RenameActionOptions { get; } = ["Move", "Copy", "Hard Link", "Symlink", "Test"];

    private static readonly string[] DatasourceValues = ["auto", "tmdb", "tvdb", "anidb", "musicbrainz"];
    private static readonly string[] LanguageCodes = ["en", "ja", "de", "fr", "es", "ko", "zh", "pt", "it", "ru"];
    private static readonly string[] EpisodeOrderValues = ["airdate", "dvd", "absolute"];
    private static readonly string[] MatchModeValues = ["opportunistic", "strict"];
    private static readonly RenameAction[] RenameActionValues = [RenameAction.Move, RenameAction.Copy, RenameAction.Hardlink, RenameAction.Symlink, RenameAction.Test];

    /// <summary>
    /// Populates all ViewModel properties from an existing preset definition.
    /// </summary>
    public void LoadFromPreset(PresetDefinitionSettings preset)
    {
        Name = preset.Name;
        InputFolder = preset.InputFolder;
        IncludeFilter = preset.IncludeFilter;
        RenamePattern = preset.RenamePattern;
        OutputFolder = preset.OutputFolder;
        KeyboardShortcut = preset.KeyboardShortcut;

        SelectedDatasourceIndex = Math.Max(0, Array.IndexOf(DatasourceValues, preset.Datasource));
        SelectedLanguageIndex = Math.Max(0, Array.IndexOf(LanguageCodes, preset.Language));
        SelectedEpisodeOrderIndex = Math.Max(0, Array.IndexOf(EpisodeOrderValues, preset.EpisodeOrder));
        SelectedMatchModeIndex = Math.Max(0, Array.IndexOf(MatchModeValues, preset.MatchMode));
        SelectedRenameActionIndex = Math.Max(0, Array.IndexOf(RenameActionValues, preset.RenameActionType));
    }

    /// <summary>
    /// Builds a <see cref="PresetDefinitionSettings"/> from the current ViewModel state.
    /// </summary>
    public PresetDefinitionSettings ToPreset() => new()
    {
        Name = Name,
        InputFolder = InputFolder,
        IncludeFilter = IncludeFilter,
        RenamePattern = RenamePattern,
        OutputFolder = OutputFolder,
        KeyboardShortcut = KeyboardShortcut,
        Datasource = DatasourceValues[SelectedDatasourceIndex],
        Language = LanguageCodes[SelectedLanguageIndex],
        EpisodeOrder = EpisodeOrderValues[SelectedEpisodeOrderIndex],
        MatchMode = MatchModeValues[SelectedMatchModeIndex],
        RenameActionType = RenameActionValues[SelectedRenameActionIndex],
    };
}
