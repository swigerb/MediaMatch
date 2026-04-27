using System.Net;
using System.Text.Json;
using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MediaMatch.Infrastructure.Tests.Providers;

public sealed class OpenAiProviderTests
{
    private static LlmConfiguration OpenAiConfig => new()
    {
        Provider = LlmProviderType.OpenAI,
        OpenAiApiKey = "sk-test123",
        OpenAiModel = "gpt-4o",
        SystemPrompt = "You are a helper.",
        MaxTokens = 100
    };

    private static MediaContext TestContext => new(
        "Movie.2024.1080p.mkv", "Movie", "The Movie", null, null, 2024, "1080p", null);

    private static Mock<HttpMessageHandler> CreateHandler(string responseContent, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseContent)
            });
        return handler;
    }

    private static string BuildChatResponse(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content } } } });

    [Fact]
    public void Name_ShouldBeOpenAI()
    {
        var handler = CreateHandler("{}");
        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);
        provider.Name.Should().Be("OpenAI");
    }

    [Fact]
    public void IsAvailable_WithApiKey_ReturnsTrue()
    {
        var handler = CreateHandler("{}");
        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);
        provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_NoApiKey_ReturnsFalse()
    {
        var config = new LlmConfiguration { Provider = LlmProviderType.OpenAI, OpenAiApiKey = "" };
        var handler = CreateHandler("{}");
        var provider = new OpenAiProvider(new HttpClient(handler.Object), config);
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WrongProvider_ReturnsFalse()
    {
        var config = new LlmConfiguration { Provider = LlmProviderType.Ollama, OpenAiApiKey = "sk-test" };
        var handler = CreateHandler("{}");
        var provider = new OpenAiProvider(new HttpClient(handler.Object), config);
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRenameAsync_ValidResponse_ReturnsSuggestion()
    {
        var json = BuildChatResponse("The Movie (2024).mkv");
        var handler = CreateHandler(json);
        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);

        var result = await provider.GenerateRenameAsync("test prompt", TestContext);
        result.Should().Be("The Movie (2024).mkv");
    }

    [Fact]
    public async Task GenerateRenameAsync_EmptyContent_ReturnsEmpty()
    {
        var json = BuildChatResponse("   ");
        var handler = CreateHandler(json);
        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);

        var result = await provider.GenerateRenameAsync("prompt", TestContext);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRenameAsync_HttpError_Throws()
    {
        var handler = CreateHandler("error", HttpStatusCode.InternalServerError);
        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);

        await provider.Invoking(p => p.GenerateRenameAsync("prompt", TestContext))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GenerateRenameAsync_SendsBearerToken()
    {
        var json = BuildChatResponse("result");
        var handler = CreateHandler(json);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Headers.Authorization != null &&
                    r.Headers.Authorization.Scheme == "Bearer" &&
                    r.Headers.Authorization.Parameter == "sk-test123"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new OpenAiProvider(new HttpClient(handler.Object), OpenAiConfig);
        await provider.GenerateRenameAsync("prompt", TestContext);

        handler.Protected().Verify("SendAsync", Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Authorization != null && r.Headers.Authorization.Scheme == "Bearer"),
            ItExpr.IsAny<CancellationToken>());
    }
}

public sealed class AzureOpenAiProviderTests
{
    private static LlmConfiguration AzureConfig => new()
    {
        Provider = LlmProviderType.AzureOpenAI,
        AzureOpenAiEndpoint = "https://myresource.openai.azure.com/",
        AzureOpenAiApiKey = "azure-key-123",
        AzureOpenAiDeployment = "gpt-4o",
        AzureOpenAiApiVersion = "2024-02-01",
        SystemPrompt = "You are a helper.",
        MaxTokens = 100
    };

    private static MediaContext TestContext => new(
        "Movie.2024.mkv", "Movie", "The Movie", null, null, 2024, null, null);

    [Fact]
    public void Name_ShouldBeAzureOpenAI()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), AzureConfig);
        provider.Name.Should().Be("AzureOpenAI");
    }

    [Fact]
    public void IsAvailable_AllConfigured_ReturnsTrue()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), AzureConfig);
        provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_MissingEndpoint_ReturnsFalse()
    {
        var config = new LlmConfiguration
        {
            Provider = LlmProviderType.AzureOpenAI,
            AzureOpenAiEndpoint = "",
            AzureOpenAiApiKey = "key",
            AzureOpenAiDeployment = "deploy"
        };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), config);
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRenameAsync_ValidResponse_ReturnsSuggestion()
    {
        var json = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { role = "assistant", content = "Clean Name.mkv" } } }
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), AzureConfig);
        var result = await provider.GenerateRenameAsync("prompt", TestContext);
        result.Should().Be("Clean Name.mkv");
    }

    [Fact]
    public async Task GenerateRenameAsync_UsesApiKeyHeader()
    {
        var json = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "result" } } }
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Headers.Contains("api-key")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), AzureConfig);
        await provider.GenerateRenameAsync("prompt", TestContext);

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Headers.Contains("api-key")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GenerateRenameAsync_EmptyResponse_ReturnsEmpty()
    {
        var json = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "  " } } }
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new AzureOpenAiProvider(new HttpClient(handler.Object), AzureConfig);
        var result = await provider.GenerateRenameAsync("prompt", TestContext);
        result.Should().BeEmpty();
    }
}

public sealed class OllamaProviderTests
{
    private static LlmConfiguration OllamaConfig => new()
    {
        Provider = LlmProviderType.Ollama,
        OllamaEndpoint = "http://localhost:11434",
        OllamaModel = "llama3",
        SystemPrompt = "You are a helper.",
        MaxTokens = 100
    };

    private static MediaContext TestContext => new(
        "Show.S01E01.mkv", "TV", "Show", 1, 1, null, null, null);

    [Fact]
    public void Name_ShouldBeOllama()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        provider.Name.Should().Be("Ollama");
    }

    [Fact]
    public void IsAvailable_WhenOllamaProvider_ReturnsTrue()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenDifferentProvider_ReturnsFalse()
    {
        var config = new LlmConfiguration { Provider = LlmProviderType.OpenAI };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new OllamaProvider(new HttpClient(handler.Object), config);
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRenameAsync_ValidResponse_ReturnsSuggestion()
    {
        var json = JsonSerializer.Serialize(new { response = "Show S01E01.mkv" });
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        var result = await provider.GenerateRenameAsync("prompt", TestContext);
        result.Should().Be("Show S01E01.mkv");
    }

    [Fact]
    public async Task GenerateRenameAsync_EmptyResponse_ReturnsEmpty()
    {
        var json = JsonSerializer.Serialize(new { response = "   " });
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        var result = await provider.GenerateRenameAsync("prompt", TestContext);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRenameAsync_HttpError_Throws()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        await provider.Invoking(p => p.GenerateRenameAsync("prompt", TestContext))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GenerateRenameAsync_PostsToGenerateEndpoint()
    {
        var json = JsonSerializer.Serialize(new { response = "result" });
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri!.ToString().Contains("/api/generate") &&
                    r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var provider = new OllamaProvider(new HttpClient(handler.Object), OllamaConfig);
        await provider.GenerateRenameAsync("prompt", TestContext);

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/api/generate")),
            ItExpr.IsAny<CancellationToken>());
    }
}
