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

    private ExpressionEditorViewModel CreateViewModel() => new(_engine.Object);

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
}
