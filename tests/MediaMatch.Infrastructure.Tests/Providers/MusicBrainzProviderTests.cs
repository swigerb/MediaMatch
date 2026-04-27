using System.Net;
using System.Text.Json;
using FluentAssertions;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class MusicBrainzProviderTests
{
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
    public void Name_ShouldBeMusicBrainz()
    {
        var handler = CreateHandler("{}");
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });
        provider.Name.Should().Be("MusicBrainz");
    }

    [Fact]
    public async Task SearchAsync_WithResults_ReturnsMusicTracks()
    {
        var response = JsonSerializer.Serialize(new
        {
            recordings = new[]
            {
                new
                {
                    id = "abc-123",
                    title = "Bohemian Rhapsody",
                    length = 354000,
                    // Note: JSON property name is "artist-credit" but we serialize with plain key
                }
            }
        });

        // Use a more realistic response that matches the internal DTO
        var json = """
        {
            "recordings": [
                {
                    "id": "abc-123",
                    "title": "Bohemian Rhapsody",
                    "length": 354000,
                    "artist-credit": [
                        { "artist": { "name": "Queen" } }
                    ],
                    "releases": [
                        {
                            "title": "A Night at the Opera",
                            "date": "1975-11-21",
                            "media": [
                                {
                                    "position": 1,
                                    "track": [ { "position": 11 } ]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var handler = CreateHandler(json);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Queen", "Bohemian Rhapsody");
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Bohemian Rhapsody");
        results[0].Artist.Should().Be("Queen");
        results[0].Album.Should().Be("A Night at the Opera");
        results[0].Year.Should().Be(1975);
        results[0].TrackNumber.Should().Be(11);
        results[0].DiscNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_EmptyRecordings_ReturnsEmpty()
    {
        var json = """{ "recordings": [] }""";
        var handler = CreateHandler(json);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Unknown", "Nothing");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_NullRecordings_ReturnsEmpty()
    {
        var json = """{ "recordings": null }""";
        var handler = CreateHandler(json);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Unknown", "Nothing");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_HttpError_ReturnsEmpty()
    {
        var handler = CreateHandler("error", HttpStatusCode.InternalServerError);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Foo", "Bar");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_RecordingWithoutArtist_IsFilteredOut()
    {
        var json = """
        {
            "recordings": [
                {
                    "id": "xyz",
                    "title": "Instrumental",
                    "artist-credit": [],
                    "releases": []
                }
            ]
        }
        """;

        var handler = CreateHandler(json);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Unknown", "Instrumental");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task LookupAsync_AlwaysReturnsNull()
    {
        var handler = CreateHandler("{}");
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var result = await provider.LookupAsync("fingerprint", 300);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_DurationConversion_MillisToSeconds()
    {
        var json = """
        {
            "recordings": [
                {
                    "id": "d1",
                    "title": "Song",
                    "length": 240000,
                    "artist-credit": [ { "artist": { "name": "Artist" } } ],
                    "releases": [ { "title": "Album", "date": "2020" } ]
                }
            ]
        }
        """;

        var handler = CreateHandler(json);
        var provider = new MusicBrainzProvider(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://musicbrainz.org/ws/2/")
        });

        var results = await provider.SearchAsync("Artist", "Song");
        results[0].Duration.Should().Be(240); // 240000ms / 1000
    }
}
