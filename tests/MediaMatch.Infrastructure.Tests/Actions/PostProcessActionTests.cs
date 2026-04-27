using System.Net;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Infrastructure.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Actions;

public sealed class PlexRefreshActionTests
{
    private static PlexSettings ConfiguredSettings => new()
    {
        Url = "http://plex.test:32400",
        Token = "plex-token",
        LibrarySectionIds = ["1", "2"]
    };

    private static PlexSettings UnconfiguredSettings => new()
    {
        Url = "",
        Token = ""
    };

    private static FileOrganizationResult SuccessResult => new(
        @"C:\media\movie.mkv", @"C:\media\Movie (2024)\Movie (2024).mkv",
        0.95f, MediaType.Movie, Array.Empty<string>(), true);

    [Fact]
    public void Name_ShouldBePlexRefresh()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new PlexRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        action.Name.Should().Be("plex-refresh");
    }

    [Fact]
    public void IsAvailable_WhenConfigured_ReturnsTrue()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new PlexRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        action.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenNotConfigured_ReturnsFalse()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new PlexRefreshAction(new HttpClient(handler.Object), UnconfiguredSettings);
        action.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_RefreshesAllConfiguredSections()
    {
        var capturedUrls = new List<string>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                capturedUrls.Add(req.RequestUri!.ToString()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var action = new PlexRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.ExecuteAsync(SuccessResult);

        capturedUrls.Should().HaveCount(2);
        capturedUrls[0].Should().Contain("/library/sections/1/refresh");
        capturedUrls[1].Should().Contain("/library/sections/2/refresh");
    }

    [Fact]
    public async Task ExecuteAsync_SendsPlexToken()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Headers.GetValues("X-Plex-Token").First() == "plex-token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var action = new PlexRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.ExecuteAsync(SuccessResult);

        handler.Protected().Verify("SendAsync", Times.Exactly(2),
            ItExpr.Is<HttpRequestMessage>(r => r.Headers.Contains("X-Plex-Token")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnavailable_DoesNothing()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var action = new PlexRefreshAction(new HttpClient(handler.Object), UnconfiguredSettings);
        await action.ExecuteAsync(SuccessResult);

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_HttpError_DoesNotThrow()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var action = new PlexRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.Invoking(a => a.ExecuteAsync(SuccessResult))
            .Should().NotThrowAsync();
    }
}

public sealed class JellyfinRefreshActionTests
{
    private static JellyfinSettings ConfiguredSettings => new()
    {
        Url = "http://jellyfin.test:8096",
        ApiKey = "jf-api-key"
    };

    private static FileOrganizationResult SuccessResult => new(
        @"C:\media\show.mkv", @"C:\media\Show\S01\Show S01E01.mkv",
        0.90f, MediaType.TvSeries, Array.Empty<string>(), true);

    [Fact]
    public void Name_ShouldBeJellyfinRefresh()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        action.Name.Should().Be("jellyfin-refresh");
    }

    [Fact]
    public void IsAvailable_WhenConfigured_ReturnsTrue()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        action.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_MissingApiKey_ReturnsFalse()
    {
        var settings = new JellyfinSettings { Url = "http://localhost", ApiKey = "" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), settings);
        action.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_PostsToLibraryRefresh()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("/Library/Refresh")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.ExecuteAsync(SuccessResult);

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/Library/Refresh")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SendsEmbyAuthHeader()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Headers.Contains("X-Emby-Authorization")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.ExecuteAsync(SuccessResult);

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Headers.Contains("X-Emby-Authorization")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_HttpError_DoesNotThrow()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Down"));

        var action = new JellyfinRefreshAction(new HttpClient(handler.Object), ConfiguredSettings);
        await action.Invoking(a => a.ExecuteAsync(SuccessResult))
            .Should().NotThrowAsync();
    }
}

public sealed class CustomScriptActionTests
{
    [Fact]
    public void Name_ShouldBeCustomScript()
    {
        var action = new CustomScriptAction("nonexistent.ps1");
        action.Name.Should().Be("custom-script");
    }

    [Fact]
    public void IsAvailable_ScriptNotExists_ReturnsFalse()
    {
        var action = new CustomScriptAction(@"C:\nonexistent\path\script.ps1");
        action.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_EmptyPath_ReturnsFalse()
    {
        var action = new CustomScriptAction("");
        action.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnavailable_DoesNotThrow()
    {
        var action = new CustomScriptAction(@"C:\nonexistent\script.ps1");
        var result = new FileOrganizationResult(
            @"C:\test.mkv", @"C:\output\test.mkv",
            0.9f, MediaType.Movie, Array.Empty<string>(), true);

        await action.Invoking(a => a.ExecuteAsync(result))
            .Should().NotThrowAsync();
    }
}
