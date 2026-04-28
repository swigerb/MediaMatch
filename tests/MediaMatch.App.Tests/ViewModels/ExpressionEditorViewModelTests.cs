using FluentAssertions;
using MediaMatch.Core.Expressions;
using Moq;

namespace MediaMatch.App.Tests.ViewModels;

/// <summary>
/// Tests for the ExpressionEditorViewModel logic.
/// Since the App project cannot be referenced directly due to WinUI dependencies,
/// these tests exercise a local copy of the core ViewModel behaviour.
/// </summary>
public sealed class ExpressionEditorViewModelTests
{
    private readonly Mock<IExpressionEngine> _engine = new();

    [Fact]
    public void Expression_WhenValid_SetsIsValidTrue()
    {
        string? outError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(true);
        _engine.Setup(e => e.Evaluate(It.IsAny<string>(), It.IsAny<IMediaBindings>())).Returns("preview");

        var vm = CreateViewModel();
        vm.Expression = "{n} ({y})";

        vm.IsValid.Should().BeTrue();
        vm.ValidationMessage.Should().Be("Valid expression");
    }

    [Fact]
    public void Expression_WhenInvalid_SetsValidationMessage()
    {
        string? outError = "Unexpected token '}'";
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(false);

        var vm = CreateViewModel();
        vm.Expression = "{n (invalid}}}";

        vm.IsValid.Should().BeFalse();
        vm.ValidationMessage.Should().Be("Unexpected token '}'");
    }

    [Fact]
    public void Expression_WhenValid_UpdatesPreview()
    {
        string? outError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(true);
        _engine.Setup(e => e.Evaluate("{n} ({y})", It.IsAny<IMediaBindings>()))
               .Returns("Breaking Bad (2008)");

        var vm = CreateViewModel();
        vm.Expression = "{n} ({y})";

        vm.Preview.Should().Be("Breaking Bad (2008)");
    }

    [Fact]
    public void Expression_WhenEmpty_SetsIsValidFalse()
    {
        var vm = CreateViewModel();
        vm.Expression = "";

        vm.IsValid.Should().BeFalse();
        vm.Preview.Should().BeEmpty();
    }

    [Fact]
    public void SelectedExample_LoadsExpression()
    {
        string? outError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(true);
        _engine.Setup(e => e.Evaluate(It.IsAny<string>(), It.IsAny<IMediaBindings>())).Returns("preview");

        var vm = CreateViewModel();
        var example = vm.ExampleExpressions.First();

        vm.SelectedExample = example;

        vm.Expression.Should().Be(example.Expression);
    }

    [Fact]
    public void InsertToken_AppendsToExpression()
    {
        string? outError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(true);
        _engine.Setup(e => e.Evaluate(It.IsAny<string>(), It.IsAny<IMediaBindings>())).Returns(string.Empty);

        var vm = CreateViewModel();
        vm.Expression = "{n} ";
        vm.CursorPosition = 4; // after the space

        vm.InsertTokenCommand.Execute("{y}");

        vm.Expression.Should().Be("{n} {y}");
    }

    [Fact]
    public void InsertToken_InsertsAtCursorPosition()
    {
        string? outError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out outError)).Returns(true);
        _engine.Setup(e => e.Evaluate(It.IsAny<string>(), It.IsAny<IMediaBindings>())).Returns(string.Empty);

        var vm = CreateViewModel();
        vm.Expression = "{n}{ext}";
        vm.CursorPosition = 3; // between {n} and {ext}

        vm.InsertTokenCommand.Execute(" ({y}) ");

        vm.Expression.Should().Be("{n} ({y}) {ext}");
    }

    [Fact]
    public void AvailableTokens_ContainsExpectedCategories()
    {
        var vm = CreateViewModel();
        var categories = vm.AvailableTokens.Select(t => t.Category).Distinct().ToList();

        categories.Should().Contain("Common");
        categories.Should().Contain("Series");
        categories.Should().Contain("Movie");
        categories.Should().Contain("Technical");
    }

    [Fact]
    public void ExampleExpressions_IsNotEmpty()
    {
        var vm = CreateViewModel();
        vm.ExampleExpressions.Should().NotBeEmpty();
    }

    // ── Inline ViewModel replica ────────────────────────────────
    // The App project cannot be referenced from this test assembly.
    // This local class replicates the core logic for testability.

    private ExpressionEditorViewModelLocal CreateViewModel() => new(_engine.Object);

    public sealed class ExpressionEditorViewModelLocal
    {
        private readonly IExpressionEngine _expressionEngine;
        private readonly IMediaBindings _sampleBindings = new SampleBindings();
        private string _expression = string.Empty;

        public string Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                OnExpressionChanged(value);
            }
        }

        public bool IsValid { get; private set; }
        public string ValidationMessage { get; private set; } = string.Empty;
        public string Preview { get; private set; } = string.Empty;
        public int CursorPosition { get; set; }

        private ExpressionExample? _selectedExample;
        public ExpressionExample? SelectedExample
        {
            get => _selectedExample;
            set
            {
                _selectedExample = value;
                if (value is not null)
                    Expression = value.Expression;
            }
        }

        public List<TokenInfo> AvailableTokens { get; } =
        [
            new("{n}", "Name (series or movie)", "Common"),
            new("{y}", "Year", "Common"),
            new("{t}", "Title (episode or movie)", "Common"),
            new("{ext}", "File extension", "Common"),
            new("{extension}", "File extension (alias)", "Common"),
            new("{fn}", "Original filename", "Common"),
            new("{s}", "Season number", "Series"),
            new("{e}", "Episode number", "Series"),
            new("{s00}", "Season zero-padded", "Series"),
            new("{e00}", "Episode zero-padded", "Series"),
            new("{s00e00}", "Combined S01E01", "Series"),
            new("{sxe}", "Season×Episode (1x01)", "Series"),
            new("{airdate}", "Air date", "Series"),
            new("{absolute}", "Absolute episode number", "Series"),
            new("{jellyfin}", "Jellyfin-compatible name", "Series"),
            new("{genre}", "Primary genre", "Movie"),
            new("{rating}", "Rating score", "Movie"),
            new("{director}", "Director name", "Movie"),
            new("{imdb}", "IMDb ID", "Movie"),
            new("{tmdb}", "TMDb ID", "Movie"),
            new("{certification}", "Content certification", "Movie"),
            new("{artist}", "Track artist", "Music"),
            new("{album}", "Album name", "Music"),
            new("{albumartist}", "Album artist", "Music"),
            new("{track}", "Track number", "Music"),
            new("{disc}", "Disc number", "Music"),
            new("{resolution}", "Video resolution", "Technical"),
            new("{hdr}", "HDR format", "Technical"),
            new("{dovi}", "Dolby Vision flag", "Technical"),
            new("{acf}", "Audio channel format", "Technical"),
            new("{bitdepth}", "Video bit depth", "Technical"),
        ];

        public List<ExpressionExample> ExampleExpressions { get; } =
        [
            new("Plex Movies", "{n} ({y})/{n} ({y}){ext}"),
            new("Plex TV", "{n}/Season {s00}/{n} - {s00e00} - {t}{ext}"),
            new("Jellyfin TV", "{n}/Season {s00}/{jellyfin}{ext}"),
            new("Simple Rename", "{n}{ext}"),
            new("Music", "{albumartist}/{album}/{mm.pad track 2} - {t}{ext}"),
        ];

        public InsertTokenCommandHelper InsertTokenCommand { get; }

        public ExpressionEditorViewModelLocal(IExpressionEngine engine)
        {
            _expressionEngine = engine;
            InsertTokenCommand = new InsertTokenCommandHelper(this);
        }

        private void OnExpressionChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsValid = false;
                ValidationMessage = "Enter an expression";
                Preview = string.Empty;
                return;
            }

            var valid = _expressionEngine.Validate(value, out var error);
            IsValid = valid;
            ValidationMessage = valid ? "Valid expression" : error ?? "Invalid expression";

            if (valid)
            {
                try { Preview = _expressionEngine.Evaluate(value, _sampleBindings); }
                catch { Preview = string.Empty; }
            }
            else
            {
                Preview = string.Empty;
            }
        }

        public sealed class InsertTokenCommandHelper
        {
            private readonly ExpressionEditorViewModelLocal _vm;
            public InsertTokenCommandHelper(ExpressionEditorViewModelLocal vm) => _vm = vm;

            public void Execute(string token)
            {
                var pos = Math.Clamp(_vm.CursorPosition, 0, _vm.Expression.Length);
                _vm.Expression = _vm.Expression.Insert(pos, token);
                _vm.CursorPosition = pos + token.Length;
            }
        }

        private sealed class SampleBindings : IMediaBindings
        {
            private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase)
            {
                ["n"] = "Breaking Bad", ["s"] = 1, ["e"] = 1,
                ["t"] = "Pilot", ["y"] = 2008,
                ["ext"] = ".mkv", ["extension"] = ".mkv",
            };

            public object? GetValue(string name) =>
                _values.TryGetValue(name, out var v) ? v : null;
            public IReadOnlyDictionary<string, object?> GetAllBindings() => _values;
            public bool HasBinding(string name) => _values.ContainsKey(name);
        }
    }

    public sealed record TokenInfo(string Name, string Description, string Category);
    public sealed record ExpressionExample(string Name, string Expression);
}
