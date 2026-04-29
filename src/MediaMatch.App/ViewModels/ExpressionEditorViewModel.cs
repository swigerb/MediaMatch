using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Expressions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Expression Editor dialog — provides live validation,
/// preview, token reference, category-specific example templates, and
/// category-specific sample bindings (FileBot-style).
/// </summary>
public partial class ExpressionEditorViewModel : ViewModelBase
{
    /// <summary>Category name for TV episode formatting.</summary>
    public const string CategoryTv = "TV Episodes";

    /// <summary>Category name for movie formatting.</summary>
    public const string CategoryMovies = "Movies";

    /// <summary>Category name for anime formatting.</summary>
    public const string CategoryAnime = "Anime";

    /// <summary>Category name for music formatting.</summary>
    public const string CategoryMusic = "Music";

    /// <summary>Token filter value showing all tokens.</summary>
    public const string TokenFilterAll = "All";

    private readonly IExpressionEngine _expressionEngine;
    private readonly Dictionary<string, IMediaBindings> _categoryBindings;
    private readonly Dictionary<string, IReadOnlyList<string>> _categoryExamples;
    private readonly Dictionary<string, string> _categoryTokenGroup;
    private IMediaBindings _activeBindings;

    /// <summary>Gets or sets the expression text being edited.</summary>
    [ObservableProperty]
    public partial string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the current expression is valid.</summary>
    [ObservableProperty]
    public partial bool IsValid { get; set; }

    /// <summary>Gets or sets the validation message for the current expression.</summary>
    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the live preview output of the current expression.</summary>
    [ObservableProperty]
    public partial string Preview { get; set; } = string.Empty;

    /// <summary>Gets or sets the currently selected example expression template.</summary>
    [ObservableProperty]
    public partial ExpressionExample? SelectedExample { get; set; }

    /// <summary>Gets or sets the cursor position in the expression text box.</summary>
    [ObservableProperty]
    public partial int CursorPosition { get; set; }

    /// <summary>Gets or sets the active formatting category (TV, Movies, Anime, Music).</summary>
    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = CategoryTv;

    /// <summary>Gets or sets the current token filter (e.g., All, Common, Series, ...).</summary>
    [ObservableProperty]
    public partial string SelectedTokenFilter { get; set; } = TokenFilterAll;

    /// <summary>Gets the available formatting categories.</summary>
    public ObservableCollection<string> Categories { get; } =
        new([CategoryTv, CategoryMovies, CategoryAnime, CategoryMusic]);

    /// <summary>Gets the available token category filters.</summary>
    public ObservableCollection<string> TokenFilters { get; } =
        new([TokenFilterAll, "Common", "Series", "Movie", "Music", "Technical"]);

    /// <summary>Gets the complete list of tokens available across all categories.</summary>
    public ObservableCollection<TokenInfo> AvailableTokens { get; } = new(BuildTokenList());

    /// <summary>Gets the list of tokens shown for the current filter / category.</summary>
    public ObservableCollection<TokenInfo> FilteredTokens { get; } = new();

    /// <summary>Gets the example templates for the current category, with pre-computed previews.</summary>
    public ObservableCollection<ExpressionExample> ExampleExpressions { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEditorViewModel"/> class.
    /// </summary>
    /// <param name="expressionEngine">The expression evaluation engine.</param>
    public ExpressionEditorViewModel(IExpressionEngine expressionEngine)
    {
        _expressionEngine = expressionEngine;

        _categoryBindings = new(StringComparer.Ordinal)
        {
            [CategoryTv] = BuildTvBindings(),
            [CategoryMovies] = BuildMovieBindings(),
            [CategoryAnime] = BuildAnimeBindings(),
            [CategoryMusic] = BuildMusicBindings(),
        };

        _categoryExamples = new(StringComparer.Ordinal)
        {
            [CategoryTv] =
            [
                "{n} - {s00e00} - {t}",
                "{n} - {sxe} - {t}",
                "{n} [{airdate}] {t}",
                "{n}/Season {s00}/{n} - {s00e00} - {t}{ext}",
                "{n}/Season {s00}/{jellyfin}{ext}",
                "{n}/{n} - {s00e00} - {t}{ext}",
            ],
            [CategoryMovies] =
            [
                "{n} ({y})",
                "{n} ({y})/{n} ({y}){ext}",
                "{n} ({y}) [{resolution} {acf}]",
                "{genre}/{n} ({y}){ext}",
                "{n} [{y}] {resolution} {hdr}",
            ],
            [CategoryAnime] =
            [
                "{n} - {absolute} - {t}",
                "{n}/{n} - {absolute}{ext}",
                "[{group}] {n} - {absolute} [{resolution}]",
                "{n} - {s00e00} - {t}",
            ],
            [CategoryMusic] =
            [
                "{artist}/{album}/{track} - {t}{ext}",
                "{albumartist} - {album}/{track} {t}{ext}",
                "{artist} - {t}{ext}",
                "{album}/{track} - {t}",
            ],
        };

        _categoryTokenGroup = new(StringComparer.Ordinal)
        {
            [CategoryTv] = "Series",
            [CategoryMovies] = "Movie",
            [CategoryAnime] = "Series",
            [CategoryMusic] = "Music",
        };

        _activeBindings = _categoryBindings[CategoryTv];
        RebuildExamples();
        RebuildFilteredTokens();
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

    partial void OnSelectedCategoryChanged(string value)
    {
        if (!_categoryBindings.TryGetValue(value, out var bindings))
            return;

        _activeBindings = bindings;
        RebuildExamples();
        RebuildFilteredTokens();
        ValidateAndPreview(Expression);
    }

    partial void OnSelectedTokenFilterChanged(string value)
    {
        RebuildFilteredTokens();
    }

    [RelayCommand]
    private void InsertToken(string token)
    {
        var pos = Math.Clamp(CursorPosition, 0, Expression.Length);
        Expression = Expression.Insert(pos, token);
        CursorPosition = pos + token.Length;
    }

    private void RebuildExamples()
    {
        ExampleExpressions.Clear();
        if (!_categoryExamples.TryGetValue(SelectedCategory, out var exprs))
            return;

        foreach (var expr in exprs)
        {
            var preview = SafeEvaluate(expr, _activeBindings);
            ExampleExpressions.Add(new ExpressionExample(expr, expr, preview));
        }
    }

    private void RebuildFilteredTokens()
    {
        FilteredTokens.Clear();

        // When a specific filter is chosen, honor it. "All" shows Common + Technical
        // plus the token group most relevant to the active category.
        IEnumerable<TokenInfo> source = SelectedTokenFilter switch
        {
            TokenFilterAll => AvailableTokens.Where(t =>
                t.Category == "Common" ||
                t.Category == "Technical" ||
                t.Category == _categoryTokenGroup.GetValueOrDefault(SelectedCategory, "Series")),
            _ => AvailableTokens.Where(t => t.Category == SelectedTokenFilter),
        };

        foreach (var t in source)
            FilteredTokens.Add(t);
    }

    private string SafeEvaluate(string expression, IMediaBindings bindings)
    {
        try
        {
            if (!_expressionEngine.Validate(expression, out _))
                return string.Empty;
            return _expressionEngine.Evaluate(expression, bindings);
        }
        catch
        {
            return string.Empty;
        }
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
                Preview = _expressionEngine.Evaluate(expression, _activeBindings);
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
        new("{group}", "Release group (anime)", "Series"),

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

    private static IMediaBindings BuildTvBindings() => new DictionaryBindings(new()
    {
        ["n"] = "Firefly",
        ["s"] = 1,
        ["e"] = 1,
        ["s00"] = "01",
        ["e00"] = "01",
        ["s00e00"] = "S01E01",
        ["sxe"] = "1x01",
        ["t"] = "Serenity",
        ["y"] = 2002,
        ["airdate"] = "2002-12-20",
        ["absolute"] = 1,
        ["ext"] = ".mkv",
        ["extension"] = ".mkv",
        ["fn"] = "firefly.s01e01",
        ["file"] = "firefly.s01e01.mkv",
        ["jellyfin"] = "Firefly - S01E01 - Serenity",
        ["resolution"] = "1080p",
        ["hdr"] = null,
        ["dovi"] = false,
        ["acf"] = "5.1",
        ["bitdepth"] = 10,
        ["startEpisode"] = 1,
        ["endEpisode"] = 1,
        ["isMultiEpisode"] = false,
    });

    private static IMediaBindings BuildMovieBindings() => new DictionaryBindings(new()
    {
        ["n"] = "The Matrix",
        ["t"] = "The Matrix",
        ["y"] = 1999,
        ["ext"] = ".mkv",
        ["extension"] = ".mkv",
        ["fn"] = "the.matrix.1999",
        ["file"] = "the.matrix.1999.mkv",
        ["genre"] = "Science Fiction",
        ["rating"] = 8.7,
        ["director"] = "The Wachowskis",
        ["imdb"] = "tt0133093",
        ["tmdb"] = "603",
        ["certification"] = "R",
        ["resolution"] = "1080p",
        ["hdr"] = "HDR10",
        ["dovi"] = false,
        ["acf"] = "5.1",
        ["bitdepth"] = 10,
    });

    private static IMediaBindings BuildAnimeBindings() => new DictionaryBindings(new()
    {
        ["n"] = "Cowboy Bebop",
        ["s"] = 1,
        ["e"] = 5,
        ["s00"] = "01",
        ["e00"] = "05",
        ["s00e00"] = "S01E05",
        ["sxe"] = "1x05",
        ["t"] = "Ballad of Fallen Angels",
        ["y"] = 1998,
        ["absolute"] = 5,
        ["ext"] = ".mkv",
        ["extension"] = ".mkv",
        ["fn"] = "[THORA] Cowboy Bebop - 05",
        ["file"] = "[THORA] Cowboy Bebop - 05.mkv",
        ["resolution"] = "720p",
        ["group"] = "THORA",
        ["acf"] = "2.0",
        ["bitdepth"] = 8,
    });

    private static IMediaBindings BuildMusicBindings() => new DictionaryBindings(new()
    {
        ["n"] = "Time",
        ["t"] = "Time",
        ["y"] = 1973,
        ["ext"] = ".flac",
        ["extension"] = ".flac",
        ["fn"] = "04 - Time",
        ["file"] = "04 - Time.flac",
        ["artist"] = "Pink Floyd",
        ["album"] = "The Dark Side of the Moon",
        ["albumartist"] = "Pink Floyd",
        ["track"] = 4,
        ["disc"] = 1,
    });

    /// <summary>Lightweight dictionary-backed bindings used for live preview samples.</summary>
    private sealed class DictionaryBindings : IMediaBindings
    {
        private readonly Dictionary<string, object?> _values;

        public DictionaryBindings(Dictionary<string, object?> values)
        {
            _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        }

        public object? GetValue(string name) =>
            _values.TryGetValue(name, out var value) ? value : null;

        public IReadOnlyDictionary<string, object?> GetAllBindings() => _values;

        public bool HasBinding(string name) => _values.ContainsKey(name);
    }
}

/// <summary>
/// A named example expression template with a pre-computed preview.
/// </summary>
/// <param name="Name">The example display name.</param>
/// <param name="Expression">The expression template string.</param>
/// <param name="Preview">The pre-evaluated preview against category bindings.</param>
public sealed record ExpressionExample(string Name, string Expression, string Preview);
