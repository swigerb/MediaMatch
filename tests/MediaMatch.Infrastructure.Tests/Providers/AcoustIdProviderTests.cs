using System.Net;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class AcoustIdProviderTests
{
    private static ApiKeySettings WithKey => new() { AcoustIdApiKey = "test-key" };
    private static ApiKeySettings NoKey => new() { AcoustIdApiKey = "" };

    private static Mock<HttpMessageHandler> CreateHandler(string responseContent, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        return handler;
    }

    [Fact]
    public void Name_ShouldBeAcoustID()
    {
        var handler = CreateHandler("{}");
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);
        provider.Name.Should().Be("AcoustID");
    }

    [Fact]
    public async Task LookupAsync_WithValidResult_ReturnsMusicTrack()
    {
        var json = """
        {
            "results": [
                {
                    "score": 0.95,
                    "recordings": [
                        {
                            "id": "rec-123",
                            "title": "Stairway to Heaven",
                            "artists": [ { "name": "Led Zeppelin" } ],
                            "releasegroups": [
                                { "title": "Led Zeppelin IV", "firstreleasedate": "1971-11-08" }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var handler = CreateHandler(json);
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var result = await provider.LookupAsync("fingerprint123", 480);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Stairway to Heaven");
        result.Artist.Should().Be("Led Zeppelin");
        result.Album.Should().Be("Led Zeppelin IV");
        result.Year.Should().Be(1971);
        result.Duration.Should().Be(480);
    }

    [Fact]
    public async Task LookupAsync_NoResults_ReturnsNull()
    {
        var json = """{ "results": [] }""";
        var handler = CreateHandler(json);
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var result = await provider.LookupAsync("fp", 100);
        result.Should().BeNull();
    }

    [Fact]
    public async Task LookupAsync_NoRecordings_ReturnsNull()
    {
        var json = """{ "results": [ { "score": 0.9, "recordings": [] } ] }""";
        var handler = CreateHandler(json);
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var result = await provider.LookupAsync("fp", 100);
        result.Should().BeNull();
    }

    [Fact]
    public async Task LookupAsync_NoApiKey_ReturnsNull()
    {
        var handler = CreateHandler("should not be called");
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            NoKey);

        var result = await provider.LookupAsync("fp", 100);
        result.Should().BeNull();

        // Verify no HTTP call was made
        handler.Protected()
            .Verify("SendAsync", Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LookupAsync_HttpError_ReturnsNull()
    {
        var handler = CreateHandler("error", HttpStatusCode.InternalServerError);
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var result = await provider.LookupAsync("fp", 100);
        result.Should().BeNull();
    }

    [Fact]
    public async Task LookupAsync_PicksHighestScoringResult()
    {
        var json = """
        {
            "results": [
                {
                    "score": 0.5,
                    "recordings": [
                        { "id": "low", "title": "Wrong Song", "artists": [ { "name": "Low" } ] }
                    ]
                },
                {
                    "score": 0.99,
                    "recordings": [
                        { "id": "high", "title": "Right Song", "artists": [ { "name": "High" } ] }
                    ]
                }
            ]
        }
        """;

        var handler = CreateHandler(json);
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var result = await provider.LookupAsync("fp", 200);
        result!.Title.Should().Be("Right Song");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty()
    {
        var handler = CreateHandler("{}");
        var provider = new AcoustIdProvider(
            new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.acoustid.org/v2/") },
            WithKey);

        var results = await provider.SearchAsync("Artist", "Title");
        results.Should().BeEmpty();
    }
}
