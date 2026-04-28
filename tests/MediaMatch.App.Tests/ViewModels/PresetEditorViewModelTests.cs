using FluentAssertions;
using MediaMatch.App.ViewModels;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;

namespace MediaMatch.App.Tests.ViewModels;

public sealed class PresetEditorViewModelTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var vm = new PresetEditorViewModel();

        vm.Name.Should().BeEmpty();
        vm.InputFolder.Should().BeEmpty();
        vm.IncludeFilter.Should().BeEmpty();
        vm.RenamePattern.Should().BeEmpty();
        vm.OutputFolder.Should().BeEmpty();
        vm.SelectedShortcutIndex.Should().Be(0); // "None"
        vm.SelectedDatasourceIndex.Should().Be(0);
        vm.SelectedLanguageIndex.Should().Be(0);
        vm.SelectedEpisodeOrderIndex.Should().Be(0);
        vm.SelectedMatchModeIndex.Should().Be(0);
        vm.SelectedRenameActionIndex.Should().Be(0);
    }

    [Fact]
    public void LoadFromPreset_PopulatesAllProperties()
    {
        var preset = new PresetDefinitionSettings
        {
            Name = "TV Shows → Plex",
            InputFolder = @"C:\Downloads",
            IncludeFilter = "*.mkv",
            RenamePattern = "{SeriesName}/S{Season:D2}E{Episode:D2}",
            OutputFolder = @"D:\Media\TV",
            KeyboardShortcut = "Ctrl+1",
            Datasource = "tvdb",
            Language = "ja",
            EpisodeOrder = "absolute",
            MatchMode = "strict",
            RenameActionType = RenameAction.Symlink,
        };

        var vm = new PresetEditorViewModel();
        vm.LoadFromPreset(preset);

        vm.Name.Should().Be("TV Shows → Plex");
        vm.InputFolder.Should().Be(@"C:\Downloads");
        vm.IncludeFilter.Should().Be("*.mkv");
        vm.RenamePattern.Should().Be("{SeriesName}/S{Season:D2}E{Episode:D2}");
        vm.OutputFolder.Should().Be(@"D:\Media\TV");
        vm.SelectedShortcutIndex.Should().Be(1); // "Ctrl+1"
        vm.SelectedDatasourceIndex.Should().Be(2); // TVDb
        vm.SelectedLanguageIndex.Should().Be(1);   // Japanese
        vm.SelectedEpisodeOrderIndex.Should().Be(2); // Absolute
        vm.SelectedMatchModeIndex.Should().Be(1);    // Strict
        vm.SelectedRenameActionIndex.Should().Be(3); // Symlink
    }

    [Fact]
    public void ToPreset_ReturnsCorrectValues()
    {
        var vm = new PresetEditorViewModel
        {
            Name = "Anime",
            InputFolder = @"C:\Anime",
            IncludeFilter = "*.mp4",
            RenamePattern = "{SeriesName} - {Episode}",
            OutputFolder = @"D:\Anime",
            SelectedShortcutIndex = 2, // Ctrl+2
            SelectedDatasourceIndex = 3,  // AniDB
            SelectedLanguageIndex = 1,    // Japanese
            SelectedEpisodeOrderIndex = 2, // Absolute
            SelectedMatchModeIndex = 0,    // Opportunistic
            SelectedRenameActionIndex = 1, // Copy
        };

        var preset = vm.ToPreset();

        preset.Name.Should().Be("Anime");
        preset.InputFolder.Should().Be(@"C:\Anime");
        preset.IncludeFilter.Should().Be("*.mp4");
        preset.RenamePattern.Should().Be("{SeriesName} - {Episode}");
        preset.OutputFolder.Should().Be(@"D:\Anime");
        preset.KeyboardShortcut.Should().Be("Ctrl+2");
        preset.Datasource.Should().Be("anidb");
        preset.Language.Should().Be("ja");
        preset.EpisodeOrder.Should().Be("absolute");
        preset.MatchMode.Should().Be("opportunistic");
        preset.RenameActionType.Should().Be(RenameAction.Copy);
    }

    [Fact]
    public void SelectedDatasourceIndex_MapsToString()
    {
        var vm = new PresetEditorViewModel();

        for (var i = 0; i < vm.DatasourceOptions.Length; i++)
        {
            vm.SelectedDatasourceIndex = i;
            var preset = vm.ToPreset();
            preset.Datasource.Should().NotBeNullOrEmpty(
                $"index {i} ({vm.DatasourceOptions[i]}) should map to a datasource string");
        }
    }

    [Fact]
    public void LoadFromPreset_WithUnknownDatasource_DefaultsToZero()
    {
        var preset = new PresetDefinitionSettings { Datasource = "unknown" };

        var vm = new PresetEditorViewModel();
        vm.LoadFromPreset(preset);

        vm.SelectedDatasourceIndex.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_PreservesAllValues()
    {
        var original = new PresetDefinitionSettings
        {
            Name = "Movies",
            InputFolder = @"C:\Input",
            IncludeFilter = "*.mkv, *.avi",
            RenamePattern = "{Name} ({Year})",
            OutputFolder = @"D:\Output",
            KeyboardShortcut = "Ctrl+3",
            Datasource = "tmdb",
            Language = "de",
            EpisodeOrder = "dvd",
            MatchMode = "strict",
            RenameActionType = RenameAction.Hardlink,
        };

        var vm = new PresetEditorViewModel();
        vm.LoadFromPreset(original);
        var result = vm.ToPreset();

        result.Name.Should().Be(original.Name);
        result.InputFolder.Should().Be(original.InputFolder);
        result.IncludeFilter.Should().Be(original.IncludeFilter);
        result.RenamePattern.Should().Be(original.RenamePattern);
        result.OutputFolder.Should().Be(original.OutputFolder);
        result.KeyboardShortcut.Should().Be(original.KeyboardShortcut);
        result.Datasource.Should().Be(original.Datasource);
        result.Language.Should().Be(original.Language);
        result.EpisodeOrder.Should().Be(original.EpisodeOrder);
        result.MatchMode.Should().Be(original.MatchMode);
        result.RenameActionType.Should().Be(original.RenameActionType);
    }
}
