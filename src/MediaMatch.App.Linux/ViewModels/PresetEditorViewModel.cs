using CommunityToolkit.Mvvm.ComponentModel;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;

namespace MediaMatch.App.Linux.ViewModels;

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

    /// <summary>Gets or sets the selected keyboard shortcut index.</summary>
    [ObservableProperty]
    public partial int SelectedShortcutIndex { get; set; }

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
    public string[] DatasourceOptions { get; } =
    [
        "Auto", "TheTVDB", "AniDB", "TheMovieDB::TV", "TVmaze",
        "TheMovieDB", "OMDb", "AcoustID", "ID3 Tags",
        "Exif Metadata", "Extended Attributes", "Plain File"
    ];

    /// <summary>Gets the available language labels with flag emoji.</summary>
    public string[] LanguageOptions { get; } =
    [
        "🇬🇧 English",
        "🇩🇪 German",
        "🇫🇷 French",
        "🇪🇸 Spanish",
        "🇧🇷 Brazilian Portuguese",
        "🇷🇺 Russian",
        "🇨🇳 Chinese",
        "🇹🇼 Taiwanese Chinese",
        "🇯🇵 Japanese",
        "🇯🇵 Romanized Japanese",
        "🇿🇦 Afrikaans",
        "🇦🇱 Albanian",
        "🇸🇦 Arabic",
        "🇦🇲 Armenian",
        "🇧🇬 Bulgarian",
        "🇪🇸 Catalan",
        "🇭🇷 Croatian",
        "🇨🇿 Czech",
        "🇩🇰 Danish",
        "🇳🇱 Dutch",
        "🇫🇮 Finnish",
        "🇨🇦 Canadian French",
        "🇬🇷 Greek",
        "🇮🇱 Hebrew",
        "🇮🇳 Hindi",
        "🇭🇺 Hungarian",
        "🇮🇸 Icelandic",
        "🇮🇩 Indonesian",
        "🇮🇹 Italian",
        "🇰🇷 Korean",
        "🇱🇻 Latvian",
        "🇱🇹 Lithuanian",
        "🇲🇰 Macedonian",
        "🇲🇾 Malay",
        "🇭🇰 Cantonese",
        "🇳🇴 Norwegian",
        "🇮🇷 Persian",
        "🇵🇱 Polish",
        "🇵🇹 Portuguese",
        "🇷🇴 Romanian",
        "🇷🇸 Serbian",
        "🇸🇰 Slovak",
        "🇸🇮 Slovenian",
        "🇲🇽 Mexican Spanish",
        "🇸🇪 Swedish",
        "🇹🇭 Thai",
        "🇹🇷 Turkish",
        "🇺🇦 Ukrainian",
        "🇻🇳 Vietnamese"
    ];

    /// <summary>Gets the available episode ordering labels.</summary>
    public string[] EpisodeOrderOptions { get; } = ["Airdate", "DVD", "Absolute", "Date and Title"];

    /// <summary>Gets the available match mode labels.</summary>
    public string[] MatchModeOptions { get; } = ["Opportunistic", "Strict"];

    /// <summary>Gets the available rename action labels.</summary>
    public string[] RenameActionOptions { get; } = ["Rename", "Copy", "Keeplink", "Symlink", "Hardlink"];

    /// <summary>Gets the available keyboard shortcut labels.</summary>
    public string[] KeyboardShortcutOptions { get; } =
    [
        "None",
        "Ctrl+1", "Ctrl+2", "Ctrl+3", "Ctrl+4", "Ctrl+5", "Ctrl+6", "Ctrl+7", "Ctrl+8", "Ctrl+9",
        "Ctrl+NumPad 1", "Ctrl+NumPad 2", "Ctrl+NumPad 3", "Ctrl+NumPad 4", "Ctrl+NumPad 5",
        "Ctrl+NumPad 6", "Ctrl+NumPad 7", "Ctrl+NumPad 8", "Ctrl+NumPad 9"
    ];

    private static readonly string[] ShortcutValues =
    [
        "",
        "Ctrl+1", "Ctrl+2", "Ctrl+3", "Ctrl+4", "Ctrl+5", "Ctrl+6", "Ctrl+7", "Ctrl+8", "Ctrl+9",
        "Ctrl+NumPad1", "Ctrl+NumPad2", "Ctrl+NumPad3", "Ctrl+NumPad4", "Ctrl+NumPad5",
        "Ctrl+NumPad6", "Ctrl+NumPad7", "Ctrl+NumPad8", "Ctrl+NumPad9"
    ];

    private static readonly string[] DatasourceValues =
    [
        "auto", "tvdb", "anidb", "tmdb_tv", "tvmaze",
        "tmdb", "omdb", "acoustid", "id3",
        "exif", "xattr", "plain"
    ];

    private static readonly string[] LanguageCodes =
    [
        "en", "de", "fr", "es", "pt-BR", "ru", "zh", "zh-TW", "ja", "ja-Latn",
        "af", "sq", "ar", "hy", "bg", "ca", "hr", "cs", "da", "nl",
        "fi", "fr-CA", "el", "he", "hi", "hu", "is", "id", "it", "ko",
        "lv", "lt", "mk", "ms", "yue", "no", "fa", "pl", "pt", "ro",
        "sr", "sk", "sl", "es-MX", "sv", "th", "tr", "uk", "vi"
    ];

    private static readonly string[] EpisodeOrderValues = ["airdate", "dvd", "absolute", "date_title"];
    private static readonly string[] MatchModeValues = ["opportunistic", "strict"];
    private static readonly RenameAction[] RenameActionValues = [RenameAction.Move, RenameAction.Copy, RenameAction.Reflink, RenameAction.Symlink, RenameAction.Hardlink];

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

        SelectedDatasourceIndex = Math.Max(0, Array.IndexOf(DatasourceValues, preset.Datasource));
        SelectedLanguageIndex = Math.Max(0, Array.IndexOf(LanguageCodes, preset.Language));
        SelectedEpisodeOrderIndex = Math.Max(0, Array.IndexOf(EpisodeOrderValues, preset.EpisodeOrder));
        SelectedMatchModeIndex = Math.Max(0, Array.IndexOf(MatchModeValues, preset.MatchMode));
        SelectedRenameActionIndex = Math.Max(0, Array.IndexOf(RenameActionValues, preset.RenameActionType));
        SelectedShortcutIndex = Math.Max(0, Array.IndexOf(ShortcutValues, preset.KeyboardShortcut));
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
        KeyboardShortcut = ShortcutValues[SelectedShortcutIndex],
        Datasource = DatasourceValues[SelectedDatasourceIndex],
        Language = LanguageCodes[SelectedLanguageIndex],
        EpisodeOrder = EpisodeOrderValues[SelectedEpisodeOrderIndex],
        MatchMode = MatchModeValues[SelectedMatchModeIndex],
        RenameActionType = RenameActionValues[SelectedRenameActionIndex],
    };
}
