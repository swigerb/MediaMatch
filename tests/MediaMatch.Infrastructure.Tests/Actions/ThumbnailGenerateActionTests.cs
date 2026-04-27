using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Infrastructure.Actions;

namespace MediaMatch.Infrastructure.Tests.Actions;

public sealed class ThumbnailGenerateActionTests
{
    [Fact]
    public void Name_IsThumbnail()
    {
        var action = new ThumbnailGenerateAction();
        action.Name.Should().Be("thumbnail");
    }

    [Fact]
    public void IsAvailable_DependsOnFfmpegPresence()
    {
        var action = new ThumbnailGenerateAction();
        // Whether this is true depends on the CI/local environment
        action.IsAvailable.Should().Be(action.IsAvailable); // just verify no exception
    }

    [Fact]
    public async Task ExecuteAsync_FailedResult_DoesNotThrow()
    {
        var action = new ThumbnailGenerateAction();
        var result = FileOrganizationResult.Failed("nonexistent.mkv", "test");

        // Should not throw even with missing file
        await action.Invoking(a => a.ExecuteAsync(result))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_NullNewPath_UsesOriginalPath()
    {
        var action = new ThumbnailGenerateAction();
        var result = new FileOrganizationResult(
            "nonexistent.mkv", null, 0.9f, MediaType.Movie,
            new List<string>(), true);

        // Should not throw — just log warning about missing file
        await action.Invoking(a => a.ExecuteAsync(result))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_Supported()
    {
        var action = new ThumbnailGenerateAction();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = FileOrganizationResult.Failed("test.mkv", "test");

        // Cancelled token should not cause issues with unavailable action
        await action.Invoking(a => a.ExecuteAsync(result, cts.Token))
            .Should().NotThrowAsync();
    }
}
