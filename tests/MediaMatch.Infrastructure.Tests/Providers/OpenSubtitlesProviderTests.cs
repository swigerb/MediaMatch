using System.Net;
using System.Text;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Infrastructure.Http;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class OpenSubtitlesProviderTests
{
    private static readonly ApiConfiguration DefaultConfig = new();
    private static readonly ApiKeySettings ConfiguredKeys = new() { OpenSubtitlesApiKey = "test-key" };
    private static readonly ApiKeySettings EmptyKeys = new() { OpenSubtitlesApiKey = "" };

    private static OpenSubtitlesProvider CreateProvider(HttpMessageHandler handler, ApiKeySettings? keys = null)
    {
        var httpClient = new HttpClient(handler);
        var mmHttp = new MediaMatchHttpClient(
            httpClient,
            NullLogger<MediaMatchHttpClient>.Instance,
            maxRetries: 0);
        return new OpenSubtitlesProvider(
            mmHttp,
            DefaultConfig,
            keys ?? ConfiguredKeys,
            NullLogger<OpenSubtitlesProvider>.Instance);
    }

    private static Mock<HttpMessageHandler> CreateVerifiableMockHandler(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            })
            .Verifiable();
        return handler;
    }

    [Fact]
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object, EmptyKeys);

        var results = await provider.SearchAsync("test", "en");

        results.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SearchByHashAsync_NoApiKey_ReturnsEmpty()
    {
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object, EmptyKeys);

        var results = await provider.SearchByHashAsync("abc123", 1024, "en");

        results.Should().BeEmpty();
        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void IsConfigured_WithApiKey_ReturnsTrue()
    {
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object, ConfiguredKeys);

        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WithoutApiKey_ReturnsFalse()
    {
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object, EmptyKeys);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_WithApiKey_ReturnsResults()
    {
        const string json = """
            {
                "data": [{
                    "attributes": {
                        "release": "Test.Sub",
                        "language": "en",
                        "downloadCount": 100,
                        "format": "srt",
                        "moviehashMatch": false,
                        "files": [{"fileId": 42, "fileName": "test.srt"}]
                    }
                }]
            }
            """;
        var handler = CreateVerifiableMockHandler(HttpStatusCode.OK, json);
        var provider = CreateProvider(handler.Object, ConfiguredKeys);

        var results = await provider.SearchAsync("test movie", "en");

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Test.Sub");
        results[0].Language.Should().Be("en");
    }
}
