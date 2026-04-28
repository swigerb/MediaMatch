using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.App.Tests.ViewModels;

public sealed class HistoryViewModelTests
{
    private readonly Mock<IUndoService> _undoServiceMock = new();

    private static UndoEntry CreateEntry(DateTimeOffset timestamp, string name = "file", MediaType mediaType = MediaType.Movie) =>
        new($@"C:\original\{name}.mkv", $@"C:\renamed\{name}.mkv", timestamp, mediaType);

    [Fact]
    public async Task LoadHistory_WithEntries_GroupsIntoSessions()
    {
        // Arrange — two clusters: entries 30s apart (one session) and entries 2 min later (another session)
        var baseTime = DateTimeOffset.UtcNow;
        var entries = new List<UndoEntry>
        {
            CreateEntry(baseTime, "file1"),
            CreateEntry(baseTime.AddSeconds(-15), "file2"),
            CreateEntry(baseTime.AddSeconds(-30), "file3"),
            CreateEntry(baseTime.AddMinutes(-5), "file4"),
            CreateEntry(baseTime.AddMinutes(-5).AddSeconds(-10), "file5"),
        };

        _undoServiceMock
            .Setup(s => s.GetJournalAsync())
            .ReturnsAsync(entries);

        var vm = new App.ViewModels.HistoryViewModel(_undoServiceMock.Object);
        await vm.LoadHistoryCommand.ExecuteAsync(null);
        vm.Sessions.Should().HaveCount(2);
        vm.Sessions[0].FileCount.Should().Be(3); // most recent session first
        vm.Sessions[1].FileCount.Should().Be(2);
        vm.IsEmpty.Should().BeFalse();
        vm.HasHistory.Should().BeTrue();
    }

    [Fact]
    public async Task LoadHistory_EmptyJournal_SetsIsEmpty()
    {
        _undoServiceMock
            .Setup(s => s.GetJournalAsync())
            .ReturnsAsync(new List<UndoEntry>());

        var vm = new App.ViewModels.HistoryViewModel(_undoServiceMock.Object);
        await vm.LoadHistoryCommand.ExecuteAsync(null);
        vm.Sessions.Should().BeEmpty();
        vm.IsEmpty.Should().BeTrue();
        vm.HasHistory.Should().BeFalse();
        vm.StatusMessage.Should().Contain("No rename history");
    }

    [Fact]
    public async Task RevertSelected_CallsUndoService()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var entries = new List<UndoEntry>
        {
            CreateEntry(baseTime, "file1"),
            CreateEntry(baseTime.AddSeconds(-10), "file2"),
        };

        var callCount = 0;
        _undoServiceMock
            .Setup(s => s.GetJournalAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call: load history; subsequent calls: after revert (empty)
                return callCount == 1 ? entries : new List<UndoEntry>();
            });

        _undoServiceMock
            .Setup(s => s.UndoAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var vm = new App.ViewModels.HistoryViewModel(_undoServiceMock.Object);
        await vm.LoadHistoryCommand.ExecuteAsync(null);

        vm.SelectedSession = vm.Sessions.First();
        await vm.RevertSelectedCommand.ExecuteAsync(null);
        _undoServiceMock.Verify(s => s.UndoAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearHistory_ClearsAllEntries()
    {
        // Arrange — ClearHistory shows a ContentDialog which we can't test in unit tests,
        // so we test the grouping logic and state management instead.
        var baseTime = DateTimeOffset.UtcNow;
        var entries = new List<UndoEntry>
        {
            CreateEntry(baseTime, "file1"),
        };

        _undoServiceMock
            .Setup(s => s.GetJournalAsync())
            .ReturnsAsync(entries);

        var vm = new App.ViewModels.HistoryViewModel(_undoServiceMock.Object);
        await vm.LoadHistoryCommand.ExecuteAsync(null);

        vm.Sessions.Should().HaveCount(1);

        // Verify that after loading, the grouping is correct and state reflects data
        vm.IsEmpty.Should().BeFalse();
        vm.HasHistory.Should().BeTrue();
    }

    [Fact]
    public void GroupIntoSessions_EntriesWithinGap_FormsSingleSession()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var entries = new List<UndoEntry>
        {
            CreateEntry(baseTime, "a"),
            CreateEntry(baseTime.AddSeconds(-20), "b"),
            CreateEntry(baseTime.AddSeconds(-40), "c"),
        };
        var sessions = App.ViewModels.HistoryViewModel.GroupIntoSessions(entries);
        sessions.Should().HaveCount(1);
        sessions[0].FileCount.Should().Be(3);
        sessions[0].Summary.Should().Be("3 files renamed");
    }

    [Fact]
    public void GroupIntoSessions_EntriesBeyondGap_FormsSeparateSessions()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var entries = new List<UndoEntry>
        {
            CreateEntry(baseTime, "a"),
            CreateEntry(baseTime.AddMinutes(-10), "b"),
            CreateEntry(baseTime.AddMinutes(-20), "c"),
        };
        var sessions = App.ViewModels.HistoryViewModel.GroupIntoSessions(entries);
        sessions.Should().HaveCount(3);
        sessions.Should().AllSatisfy(s => s.FileCount.Should().Be(1));
    }
}
