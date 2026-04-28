using FluentAssertions;
using MediaMatch.App.ViewModels;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MediaMatch.App.Tests.ViewModels;

/// <summary>
/// Smoke tests verifying all panel ViewModels can be constructed and expose
/// non-null commands. These catch binding-related NullReferenceExceptions
/// that would crash the app on launch.
/// </summary>
public sealed class PanelViewModelSmokeTests
{
    [Fact]
    public void FilterPanelViewModel_DefaultConstructor_CreatesNonNullCommands()
    {
        var vm = new FilterPanelViewModel();

        vm.ClearCommand.Should().NotBeNull();
        vm.LoadMediaInfoCommand.Should().NotBeNull();
        vm.Files.Should().NotBeNull();
        vm.MediaInfoEntries.Should().NotBeNull();
    }

    [Fact]
    public void FilterPanelViewModel_WithServices_CreatesNonNullCommands()
    {
        var analysis = new Mock<IMediaAnalysisService>();
        var logger = NullLogger<FilterPanelViewModel>.Instance;

        var vm = new FilterPanelViewModel(analysis.Object, logger);

        vm.ClearCommand.Should().NotBeNull();
        vm.Files.Should().NotBeNull();
    }

    [Fact]
    public void EpisodesPanelViewModel_DefaultConstructor_CreatesNonNullCommands()
    {
        var vm = new EpisodesPanelViewModel();

        vm.FindCommand.Should().NotBeNull();
        vm.Episodes.Should().NotBeNull();
    }

    [Fact]
    public void EpisodesPanelViewModel_WithServices_CreatesNonNullCommands()
    {
        var provider = new Mock<IEpisodeProvider>();
        var logger = NullLogger<EpisodesPanelViewModel>.Instance;

        var vm = new EpisodesPanelViewModel(provider.Object, logger);

        vm.FindCommand.Should().NotBeNull();
    }

    [Fact]
    public void SubtitlePanelViewModel_DefaultConstructor_CreatesNonNullCommands()
    {
        var vm = new SubtitlePanelViewModel();

        vm.FindCommand.Should().NotBeNull();
        vm.DownloadSelectedCommand.Should().NotBeNull();
        vm.Results.Should().NotBeNull();
    }

    [Fact]
    public void SubtitlePanelViewModel_WithServices_CreatesNonNullCommands()
    {
        var provider = new Mock<ISubtitleProvider>();
        var logger = NullLogger<SubtitlePanelViewModel>.Instance;

        var vm = new SubtitlePanelViewModel(provider.Object, logger);

        vm.FindCommand.Should().NotBeNull();
        vm.DownloadSelectedCommand.Should().NotBeNull();
    }

    [Fact]
    public void ListPanelViewModel_DefaultConstructor_CreatesNonNullCommands()
    {
        var vm = new ListPanelViewModel();

        vm.ApplyPatternCommand.Should().NotBeNull();
        vm.ClearCommand.Should().NotBeNull();
        vm.Files.Should().NotBeNull();
    }

    [Fact]
    public void ListPanelViewModel_WithServices_CreatesNonNullCommands()
    {
        var engine = new Mock<IExpressionEngine>();
        var logger = NullLogger<ListPanelViewModel>.Instance;

        var vm = new ListPanelViewModel(engine.Object, logger);

        vm.ApplyPatternCommand.Should().NotBeNull();
        vm.ClearCommand.Should().NotBeNull();
    }

    [Theory]
    [InlineData(typeof(FilterPanelViewModel))]
    [InlineData(typeof(EpisodesPanelViewModel))]
    [InlineData(typeof(SubtitlePanelViewModel))]
    [InlineData(typeof(ListPanelViewModel))]
    [InlineData(typeof(PresetEditorViewModel))]
    public void AllPanelViewModels_DefaultConstructor_DoesNotThrow(Type vmType)
    {
        var act = () => Activator.CreateInstance(vmType);

        act.Should().NotThrow($"{vmType.Name} must be constructable without parameters for XAML design-time use");
    }

    [Fact]
    public void PresetEditorViewModel_DefaultConstructor_CreatesNonNullOptionArrays()
    {
        var vm = new PresetEditorViewModel();

        vm.DatasourceOptions.Should().NotBeNull().And.NotBeEmpty();
        vm.LanguageOptions.Should().NotBeNull().And.NotBeEmpty();
        vm.EpisodeOrderOptions.Should().NotBeNull().And.NotBeEmpty();
        vm.MatchModeOptions.Should().NotBeNull().And.NotBeEmpty();
        vm.RenameActionOptions.Should().NotBeNull().And.NotBeEmpty();
    }
}
