using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Http;

public sealed class MediaMatchHttpClientTests
{
    private record TestPayload(string Name, int Value);

    private static MediaMatchHttpClient CreateClient(HttpMessageHandler handler, int maxRetries = 3)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        return new MediaMatchHttpClient(httpClient, NullLogger<MediaMatchHttpClient>.Instance, maxRetries);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, object? content = null)
    {
        var mock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(statusCode);
        if (content is not null)
            response.Content = JsonContent.Create(content);

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return mock;
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponse_DeserializesJson()
    {
        var expected = new TestPayload("test", 42);
        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var sut = CreateClient(handler.Object);

        var result = await sut.GetAsync<TestPayload>("https://test.local/api");

        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_ServerError_RetriesAndThrows()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

        // maxRetries=0 means no retries — throws immediately on first 503
        var sut = CreateClient(handler.Object, maxRetries: 0);

        var act = () => sut.GetAsync<TestPayload>("https://test.local/api");

        await act.Should().ThrowAsync<HttpRequestException>();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_TooManyRequests_BacksOff()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    var response429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    response429.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(50));
                    return response429;
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new TestPayload("recovered", 1))
                };
            });

        var sut = CreateClient(handler.Object, maxRetries: 1);

        var result = await sut.GetAsync<TestPayload>("https://test.local/api");

        result.Should().NotBeNull();
        result!.Name.Should().Be("recovered");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task PostAsync_SuccessfulResponse_DeserializesJson()
    {
        var request = new TestPayload("req", 1);
        var expected = new TestPayload("resp", 2);
        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var sut = CreateClient(handler.Object);

        var result = await sut.PostAsync<TestPayload, TestPayload>("https://test.local/api", request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("resp");
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_CancellationRequested_ThrowsTaskCanceled()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, new TestPayload("x", 0));
        var sut = CreateClient(handler.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.GetAsync<TestPayload>("https://test.local/api", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAsync_NonTransientError_ThrowsImmediately()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

        var sut = CreateClient(handler.Object, maxRetries: 3);

        var act = () => sut.GetAsync<TestPayload>("https://test.local/api");

        await act.Should().ThrowAsync<HttpRequestException>();
        // Non-transient errors should not retry
        callCount.Should().Be(1);
    }
}
