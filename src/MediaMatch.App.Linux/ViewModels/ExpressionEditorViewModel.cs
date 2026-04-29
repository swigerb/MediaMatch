using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Expressions;

namespace MediaMatch.App.Linux.ViewModels;

/// <summary>
/// ViewModel for the Expression Editor dialog — provides live validation,
/// preview, token reference, and example expression templates.
/// </summary>
public partial class ExpressionEditorViewModel : ViewModelBase
{
    private readonly IExpressionEngine _expressionEngine;
    private readonly IMediaBindings _sampleBindings = new SampleMediaBindings();

    /// <summary>Gets or sets the expression text being edited.</summary>
    [ObservableProperty]
    private string _expression = string.Empty;

    /// <summary>Gets or sets a value indicating whether the current expression is valid.</summary>
    [ObservableProperty]
    private bool _isValid;
    /// <summary>Gets or sets the validation message for the current expression.</summary>
    [ObservableProperty]
    private string _validationMessage = string.Empty;

    /// <summary>Gets or sets the live preview output of the current expression.</summary>
    [ObservableProperty]
    private string _preview = string.Empty;

    /// <summary>Gets or sets the currently selected example expression template.</summary>
    [ObservableProperty]
    private ExpressionExample? _selectedExample;
    /// <summary>Gets or sets the cursor position in the expression text box.</summary>
    [ObservableProperty]
    private int _cursorPosition;
    /// <summary>Gets the list of available tokens for insertion.</summary>
    public ObservableCollection<TokenInfo> AvailableTokens { get; } = new(BuildTokenList());

    /// <summary>Gets the list of predefined example expression templates.</summary>
    public ObservableCollection<ExpressionExample> ExampleExpressions { get; } = new(BuildExampleList());

    public ExpressionEditorViewModel(IExpressionEngine expressionEngine)
    {
        _expressionEngine = expressionEngine;
    }

    partial void OnExpressionChanged(string value)
    {
        ValidateAndPreview(value);
    }

    partial void OnSelectedExampleChanged(ExpressionExample? value)
    {
        if (value is not null)
            Expression = value.Expression;
    }

    [RelayCommand]
    private void InsertToken(string token)
    {
        var pos = Math.Clamp(CursorPosition, 0, Expression.Length);
        Expression = Expression.Insert(pos, token);
        CursorPosition = pos + token.Length;
    }

    private void ValidateAndPreview(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            IsValid = false;
            ValidationMessage = "Enter an expression";
            Preview = string.Empty;
            return;
        }

        var valid = _expressionEngine.Validate(expression, out var error);
        IsValid = valid;
        ValidationMessage = valid ? "Valid expression" : error ?? "Invalid expression";

        if (valid)
        {
            try
            {
                Preview = _expressionEngine.Evaluate(expression, _sampleBindings);
            }
            catch
            {
                Preview = string.Empty;
            }
        }
        else
        {
            Preview = string.Empty;
        }
    }

    private static List<TokenInfo> BuildTokenList() =>
    [
        // Common
        new("{n}", "Name (series or movie)", "Common"),
        new("{y}", "Year", "Common"),
        new("{t}", "Title (episode or movie)", "Common"),
        new("{ext}", "File extension (e.g., .mkv)", "Common"),
        new("{extension}", "File extension (alias)", "Common"),
        new("{fn}", "Original filename without extension", "Common"),

        // Series
        new("{s}", "Season number", "Series"),
        new("{e}", "Episode number", "Series"),
        new("{s00}", "Season number zero-padded (2 digits)", "Series"),
        new("{e00}", "Episode number zero-padded (2 digits)", "Series"),
        new("{s00e00}", "Combined S01E01 format", "Series"),
        new("{sxe}", "Season×Episode format (1x01)", "Series"),
        new("{airdate}", "Episode air date", "Series"),
        new("{absolute}", "Absolute episode number", "Series"),
        new("{jellyfin}", "Jellyfin-compatible name", "Series"),

        // Movie
        new("{genre}", "Primary genre", "Movie"),
        new("{rating}", "Rating score", "Movie"),
        new("{director}", "Director name", "Movie"),
        new("{imdb}", "IMDb ID", "Movie"),
        new("{tmdb}", "TMDb ID", "Movie"),
        new("{certification}", "Content certification", "Movie"),

        // Music
        new("{artist}", "Track artist", "Music"),
        new("{album}", "Album name", "Music"),
        new("{albumartist}", "Album artist", "Music"),
        new("{track}", "Track number", "Music"),
        new("{disc}", "Disc number", "Music"),

        // Technical
        new("{resolution}", "Video resolution (e.g., 1080p)", "Technical"),
        new("{hdr}", "HDR format", "Technical"),
        new("{dovi}", "Dolby Vision flag", "Technical"),
        new("{acf}", "Audio channel format", "Technical"),
        new("{bitdepth}", "Video bit depth", "Technical"),
    ];

    private static List<ExpressionExample> BuildExampleList() =>
    [
        new("Plex Movies", "{n} ({y})/{n} ({y}){ext}"),
        new("Plex TV", "{n}/Season {s00}/{n} - {s00e00} - {t}{ext}"),
        new("Jellyfin TV", "{n}/Season {s00}/{jellyfin}{ext}"),
        new("Simple Rename", "{n}{ext}"),
        new("Music", "{albumartist}/{album}/{mm.pad track 2} - {t}{ext}"),
    ];

    private sealed class SampleMediaBindings : IMediaBindings
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["n"] = "Breaking Bad",
            ["s"] = 1,
            ["e"] = 1,
            ["s00"] = "01",
            ["e00"] = "01",
            ["s00e00"] = "S01E01",
            ["sxe"] = "1x01",
            ["t"] = "Pilot",
            ["y"] = 2008,
            ["airdate"] = "2008-01-20",
            ["absolute"] = 1,
            ["ext"] = ".mkv",
            ["extension"] = ".mkv",
            ["fn"] = "breaking.bad.s01e01.pilot",
            ["file"] = "breaking.bad.s01e01.pilot.mkv",
            ["jellyfin"] = "Breaking Bad - S01E01 - Pilot",
            ["genre"] = "Drama",
            ["rating"] = 9.5,
            ["director"] = "Vince Gilligan",
            ["imdb"] = "tt0903747",
            ["tmdb"] = "1396",
            ["certification"] = "TV-MA",
            ["resolution"] = "1080p",
            ["hdr"] = null,
            ["dovi"] = false,
            ["acf"] = "5.1",
            ["bitdepth"] = 10,
            ["artist"] = "Pink Floyd",
            ["album"] = "The Dark Side of the Moon",
            ["albumartist"] = "Pink Floyd",
            ["track"] = 1,
            ["disc"] = 1,
            ["startEpisode"] = 1,
            ["endEpisode"] = 1,
            ["isMultiEpisode"] = false,
        };

        public object? GetValue(string name) =>
            _values.TryGetValue(name, out var value) ? value : null;

        public IReadOnlyDictionary<string, object?> GetAllBindings() => _values;

        public bool HasBinding(string name) => _values.ContainsKey(name);
    }
}

/// <summary>
/// A named example expression template.
/// </summary>
/// <param name="Name">The example display name.</param>
/// <param name="Expression">The expression template string.</param>
public sealed record ExpressionExample(string Name, string Expression);
