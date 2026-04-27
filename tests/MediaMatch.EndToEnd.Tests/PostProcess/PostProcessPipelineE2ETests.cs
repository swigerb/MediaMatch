using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.PostProcess;

/// <summary>
/// E2E: Post-process pipeline — configure actions → run → verify execution order → failure isolation.
/// </summary>
public class PostProcessPipelineE2ETests
{
    private static FileOrganizationResult MakeResult(string original, string renamed) =>
        new(original, renamed, 0.9f, Core.Enums.MediaType.Movie, [], true);

    // ── Execution order ───────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_MultipleActions_ExecutedInOrder()
    {
        var executionOrder = new List<string>();

        var action1 = new Mock<IPostProcessAction>();
        action1.Setup(a => a.Name).Returns("first");
        action1.Setup(a => a.IsAvailable).Returns(true);
        action1
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("first"))
            .Returns(Task.CompletedTask);

        var action2 = new Mock<IPostProcessAction>();
        action2.Setup(a => a.Name).Returns("second");
        action2.Setup(a => a.IsAvailable).Returns(true);
        action2
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("second"))
            .Returns(Task.CompletedTask);

        var action3 = new Mock<IPostProcessAction>();
        action3.Setup(a => a.Name).Returns("third");
        action3.Setup(a => a.IsAvailable).Returns(true);
        action3
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("third"))
            .Returns(Task.CompletedTask);

        var pipeline = new PostProcessPipeline([action1.Object, action2.Object, action3.Object]);
        await pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"));

        executionOrder.Should().ContainInOrder("first", "second", "third");
    }

    // ── Failure isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_OneActionFails_OthersStillRun()
    {
        var executed = new List<string>();

        var successBefore = new Mock<IPostProcessAction>();
        successBefore.Setup(a => a.Name).Returns("before");
        successBefore.Setup(a => a.IsAvailable).Returns(true);
        successBefore
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => executed.Add("before"))
            .Returns(Task.CompletedTask);

        var failingAction = new Mock<IPostProcessAction>();
        failingAction.Setup(a => a.Name).Returns("failing");
        failingAction.Setup(a => a.IsAvailable).Returns(true);
        failingAction
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Action failed"));

        var successAfter = new Mock<IPostProcessAction>();
        successAfter.Setup(a => a.Name).Returns("after");
        successAfter.Setup(a => a.IsAvailable).Returns(true);
        successAfter
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => executed.Add("after"))
            .Returns(Task.CompletedTask);

        var pipeline = new PostProcessPipeline(
            [successBefore.Object, failingAction.Object, successAfter.Object]);

        // Should NOT throw
        var act = () => pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"));
        await act.Should().NotThrowAsync();

        executed.Should().Contain("before");
        executed.Should().Contain("after");
    }

    // ── Availability check ────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_UnavailableAction_IsSkipped()
    {
        var unavailableAction = new Mock<IPostProcessAction>();
        unavailableAction.Setup(a => a.Name).Returns("plex-refresh");
        unavailableAction.Setup(a => a.IsAvailable).Returns(false);

        var availableAction = new Mock<IPostProcessAction>();
        availableAction.Setup(a => a.Name).Returns("thumbnail");
        availableAction.Setup(a => a.IsAvailable).Returns(true);
        availableAction
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pipeline = new PostProcessPipeline([unavailableAction.Object, availableAction.Object]);
        await pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"));

        // Unavailable action should NOT be called
        unavailableAction.Verify(
            a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Available action should be called
        availableAction.Verify(
            a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Action filter ─────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_ActionFilter_OnlyRunsNamedActions()
    {
        var called = new List<string>();

        var plexAction = new Mock<IPostProcessAction>();
        plexAction.Setup(a => a.Name).Returns("plex-refresh");
        plexAction.Setup(a => a.IsAvailable).Returns(true);
        plexAction
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.Add("plex-refresh"))
            .Returns(Task.CompletedTask);

        var thumbAction = new Mock<IPostProcessAction>();
        thumbAction.Setup(a => a.Name).Returns("thumbnail");
        thumbAction.Setup(a => a.IsAvailable).Returns(true);
        thumbAction
            .Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.Add("thumbnail"))
            .Returns(Task.CompletedTask);

        var pipeline = new PostProcessPipeline([plexAction.Object, thumbAction.Object]);
        var filter = new HashSet<string> { "plex-refresh" };
        await pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"), filter);

        called.Should().ContainSingle().Which.Should().Be("plex-refresh");
    }

    [Fact]
    public async Task Pipeline_EmptyActions_DoesNotThrow()
    {
        var pipeline = new PostProcessPipeline([]);
        var act = () => pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"));
        await act.Should().NotThrowAsync();
    }

    // ── Cancellation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_Cancellation_StopsExecution()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = new Mock<IPostProcessAction>();
        action.Setup(a => a.Name).Returns("action");
        action.Setup(a => a.IsAvailable).Returns(true);

        var pipeline = new PostProcessPipeline([action.Object]);

        var act = () => pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        action.Verify(
            a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Multiple failures ─────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_AllActionsFail_DoesNotThrow()
    {
        var failingActions = Enumerable.Range(1, 3).Select(i =>
        {
            var mock = new Mock<IPostProcessAction>();
            mock.Setup(a => a.Name).Returns($"action{i}");
            mock.Setup(a => a.IsAvailable).Returns(true);
            mock.Setup(a => a.ExecuteAsync(
                    It.IsAny<FileOrganizationResult>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception($"Action {i} failed"));
            return mock.Object;
        }).ToList();

        var pipeline = new PostProcessPipeline(failingActions);
        var act = () => pipeline.ExecuteAsync(MakeResult("original.mkv", "renamed.mkv"));
        await act.Should().NotThrowAsync();
    }
}
