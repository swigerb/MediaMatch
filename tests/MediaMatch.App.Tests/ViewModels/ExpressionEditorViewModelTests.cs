using FluentAssertions;
using MediaMatch.App.ViewModels;
using MediaMatch.Core.Expressions;
using Moq;

namespace MediaMatch.App.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="ExpressionEditorViewModel"/>. The production ViewModel is linked
/// into this project (see csproj) so we exercise the real class — no inline replica.
/// </summary>
public sealed class ExpressionEditorViewModelTests
{
    private readonly Mock<IExpressionEngine> _engine = new();

    public ExpressionEditorViewModelTests()
    {
        // Default behavior: validate succeeds, evaluate echoes the expression.
        // Tests can override per-call.
        string? defaultError = null;
        _engine.Setup(e => e.Validate(It.IsAny<string>(), out defaultError)).Returns(true);
        _engine.Setup(e => e.Evaluate(It.IsAny<string>(), It.IsAny<IMediaBindings>()))
               .Returns<string, IMediaBindings>((expr, _) => expr);
    }

    private ExpressionEditorViewModel CreateViewModel() => new(_engine.Object);

    [Fact]
    public void Expression_WhenValid_SetsIsValidTrue()
    {
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
        _engine.Setup(e => e.Evaluate("{n} ({y})", It.IsAny<IMediaBindings>()))
               .Returns("Firefly (2002)");

        var vm = CreateViewModel();
        vm.Expression = "{n} ({y})";

        vm.Preview.Should().Be("Firefly (2002)");
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
        var vm = CreateViewModel();
        var example = vm.ExampleExpressions.First();

        vm.SelectedExample = example;

        vm.Expression.Should().Be(example.Expression);
    }

    [Fact]
    public void InsertToken_AppendsToExpression()
    {
        var vm = CreateViewModel();
        vm.Expression = "{n} ";
        vm.CursorPosition = 4;

        vm.InsertTokenCommand.Execute("{y}");

        vm.Expression.Should().Be("{n} {y}");
    }

    [Fact]
    public void InsertToken_InsertsAtCursorPosition()
    {
        var vm = CreateViewModel();
        vm.Expression = "{n}{ext}";
        vm.CursorPosition = 3;

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
        categories.Should().Contain("Music");
        categories.Should().Contain("Technical");
    }

    [Fact]
    public void Categories_ContainsAllFour()
    {
        var vm = CreateViewModel();
        vm.Categories.Should().BeEquivalentTo(["TV Episodes", "Movies", "Anime", "Music"]);
    }

    [Fact]
    public void DefaultCategory_IsTvEpisodes()
    {
        var vm = CreateViewModel();
        vm.SelectedCategory.Should().Be(ExpressionEditorViewModel.CategoryTv);
    }

    [Fact]
    public void ExampleExpressions_AreCategorySpecific()
    {
        var vm = CreateViewModel();
        var tvExamples = vm.ExampleExpressions.Select(e => e.Expression).ToList();
        tvExamples.Should().Contain(e => e.Contains("{s00e00}"));

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMovies;
        var movieExamples = vm.ExampleExpressions.Select(e => e.Expression).ToList();
        movieExamples.Should().NotEqual(tvExamples);
        movieExamples.Should().Contain(e => e.Contains("{y}"));
        movieExamples.Should().NotContain(e => e.Contains("{s00e00}"));

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMusic;
        var musicExamples = vm.ExampleExpressions.Select(e => e.Expression).ToList();
        musicExamples.Should().Contain(e => e.Contains("{artist}") || e.Contains("{album}"));
    }

    [Fact]
    public void ExampleExpressions_HavePrecomputedPreviews()
    {
        var vm = CreateViewModel();
        // Default mock returns the expression as the preview.
        vm.ExampleExpressions.Should().NotBeEmpty();
        vm.ExampleExpressions.Should().OnlyContain(e => !string.IsNullOrEmpty(e.Preview));
    }

    [Fact]
    public void ChangingCategory_UsesCategorySpecificBindings()
    {
        IMediaBindings? capturedBindings = null;
        _engine.Setup(e => e.Evaluate("expr", It.IsAny<IMediaBindings>()))
               .Returns<string, IMediaBindings>((_, b) =>
               {
                   capturedBindings = b;
                   return "ok";
               });

        var vm = CreateViewModel();
        vm.Expression = "expr";

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMovies;
        capturedBindings.Should().NotBeNull();
        capturedBindings!.GetValue("n").Should().Be("The Matrix");

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryAnime;
        capturedBindings!.GetValue("n").Should().Be("Cowboy Bebop");
        capturedBindings!.GetValue("group").Should().Be("THORA");

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMusic;
        capturedBindings!.GetValue("artist").Should().Be("Pink Floyd");
        capturedBindings!.GetValue("album").Should().Be("The Dark Side of the Moon");

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryTv;
        capturedBindings!.GetValue("n").Should().Be("Firefly");
        capturedBindings!.GetValue("t").Should().Be("Serenity");
    }

    [Fact]
    public void TokenFilter_All_ShowsCategoryRelevantPlusCommonAndTechnical()
    {
        var vm = CreateViewModel();
        // TV → Series group
        vm.FilteredTokens.Select(t => t.Category).Distinct()
            .Should().BeEquivalentTo(["Common", "Series", "Technical"]);

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMovies;
        vm.FilteredTokens.Select(t => t.Category).Distinct()
            .Should().BeEquivalentTo(["Common", "Movie", "Technical"]);

        vm.SelectedCategory = ExpressionEditorViewModel.CategoryMusic;
        vm.FilteredTokens.Select(t => t.Category).Distinct()
            .Should().BeEquivalentTo(["Common", "Music", "Technical"]);
    }

    [Fact]
    public void TokenFilter_Specific_OnlyShowsThatCategory()
    {
        var vm = CreateViewModel();
        vm.SelectedTokenFilter = "Movie";
        vm.FilteredTokens.Should().OnlyContain(t => t.Category == "Movie");

        vm.SelectedTokenFilter = "Music";
        vm.FilteredTokens.Should().OnlyContain(t => t.Category == "Music");
    }
}
