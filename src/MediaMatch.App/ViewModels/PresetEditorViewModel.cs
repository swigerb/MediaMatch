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
    /// <summary>Gets or sets the preset display name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the preset input folder path.</summary>
    [ObservableProperty]
    public partial string InputFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the file include filter pattern.</summary>
    [ObservableProperty]
    public partial string IncludeFilter { get; set; } = string.Empty;

    /// <summary>Gets or sets the rename pattern template.</summary>
    [ObservableProperty]
    public partial string RenamePattern { get; set; } = string.Empty;

    /// <summary>Gets or sets the output folder path for renamed files.</summary>
    [ObservableProperty]
    public partial string OutputFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the keyboard shortcut assigned to this preset.</summary>
    [ObservableProperty]
    public partial string KeyboardShortcut { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected data source index.</summary>
    [ObservableProperty]
    public partial int SelectedDatasourceIndex { get; set; }

    /// <summary>Gets or sets the selected language index.</summary>
    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    /// <summary>Gets or sets the selected episode ordering index.</summary>
    [ObservableProperty]
    public partial int SelectedEpisodeOrderIndex { get; set; }

    /// <summary>Gets or sets the selected match mode index.</summary>
    [ObservableProperty]
    public partial int SelectedMatchModeIndex { get; set; }

    /// <summary>Gets or sets the selected rename action index.</summary>
    [ObservableProperty]
    public partial int SelectedRenameActionIndex { get; set; }

    /// <summary>Gets the available data source labels.</summary>
    public string[] DatasourceOptions { get; } = ["Auto", "TMDb", "TVDb", "AniDB", "MusicBrainz"];

    /// <summary>Gets the available language labels.</summary>
    public string[] LanguageOptions { get; } = ["English", "Japanese", "German", "French", "Spanish", "Korean", "Chinese", "Portuguese", "Italian", "Russian"];

    /// <summary>Gets the available episode ordering labels.</summary>
    public string[] EpisodeOrderOptions { get; } = ["Airdate", "DVD", "Absolute"];

    /// <summary>Gets the available match mode labels.</summary>
    public string[] MatchModeOptions { get; } = ["Opportunistic", "Strict"];

    /// <summary>Gets the available rename action labels.</summary>
    public string[] RenameActionOptions { get; } = ["Move", "Copy", "Hard Link", "Symlink", "Test"];

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
