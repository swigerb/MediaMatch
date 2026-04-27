using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.Application.Tests.Services;

public sealed class PostProcessPipelineTests
{
    private static FileOrganizationResult SuccessResult => new(
        @"C:\media\movie.mkv", @"C:\media\Movie (2024)\Movie (2024).mkv",
        0.95f, MediaType.Movie, Array.Empty<string>(), true);

    private static Mock<IPostProcessAction> CreateAction(
        string name = "test-action", bool available = true, bool throws = false)
    {
        var mock = new Mock<IPostProcessAction>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.IsAvailable).Returns(available);

        if (throws)
        {
            mock.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Action failed"));
        }
        else
        {
            mock.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_RunsAllAvailableActions()
    {
        var a1 = CreateAction("action1");
        var a2 = CreateAction("action2");
        var pipeline = new PostProcessPipeline(new[] { a1.Object, a2.Object });

        await pipeline.ExecuteAsync(SuccessResult);

        a1.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
        a2.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsUnavailableActions()
    {
        var available = CreateAction("available", available: true);
        var unavailable = CreateAction("unavailable", available: false);
        var pipeline = new PostProcessPipeline(new[] { available.Object, unavailable.Object });

        await pipeline.ExecuteAsync(SuccessResult);

        available.Verify(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()), Times.Once);
        unavailable.Verify(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FailureIsolation_ContinuesOnError()
    {
        var failing = CreateAction("failing", throws: true);
        var succeeding = CreateAction("succeeding");
        var pipeline = new PostProcessPipeline(new[] { failing.Object, succeeding.Object });

        await pipeline.Invoking(p => p.ExecuteAsync(SuccessResult))
            .Should().NotThrowAsync();

        // Succeeding should still run even though failing threw
        succeeding.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExecutionOrder()
    {
        var order = new List<string>();
        var a1 = CreateAction("first");
        a1.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("first"))
            .Returns(Task.CompletedTask);
        var a2 = CreateAction("second");
        a2.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("second"))
            .Returns(Task.CompletedTask);
        var a3 = CreateAction("third");
        a3.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("third"))
            .Returns(Task.CompletedTask);

        var pipeline = new PostProcessPipeline(new[] { a1.Object, a2.Object, a3.Object });
        await pipeline.ExecuteAsync(SuccessResult);

        order.Should().Equal("first", "second", "third");
    }

    [Fact]
    public async Task ExecuteAsync_WithFilter_RunsOnlyNamedActions()
    {
        var a1 = CreateAction("plex-refresh");
        var a2 = CreateAction("jellyfin-refresh");
        var a3 = CreateAction("thumbnail");
        var pipeline = new PostProcessPipeline(new[] { a1.Object, a2.Object, a3.Object });

        var filter = new HashSet<string> { "plex-refresh", "thumbnail" };
        await pipeline.ExecuteAsync(SuccessResult, filter);

        a1.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
        a2.Verify(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()), Times.Never);
        a3.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyActions_DoesNothing()
    {
        var pipeline = new PostProcessPipeline(Array.Empty<IPostProcessAction>());
        await pipeline.Invoking(p => p.ExecuteAsync(SuccessResult))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_StopsPipeline()
    {
        var cts = new CancellationTokenSource();
        var a1 = CreateAction("first");
        a1.Setup(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .Returns(Task.CompletedTask);
        var a2 = CreateAction("second");

        var pipeline = new PostProcessPipeline(new[] { a1.Object, a2.Object });

        await pipeline.Invoking(p => p.ExecuteAsync(SuccessResult, null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        a2.Verify(a => a.ExecuteAsync(It.IsAny<FileOrganizationResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NullFilter_RunsAll()
    {
        var a1 = CreateAction("action1");
        var a2 = CreateAction("action2");
        var pipeline = new PostProcessPipeline(new[] { a1.Object, a2.Object });

        await pipeline.ExecuteAsync(SuccessResult, actionFilter: null);

        a1.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
        a2.Verify(a => a.ExecuteAsync(SuccessResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}
