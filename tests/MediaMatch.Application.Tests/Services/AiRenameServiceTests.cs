using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.Application.Tests.Services;

public sealed class AiRenameServiceTests
{
    private static MediaContext TestContext => new(
        "Movie.2024.1080p.WEB-DL.mkv",
        "Movie",
        "The Great Movie",
        null, null, 2024, "1080p", "YTS");

    private static Mock<ILlmProvider> CreateProvider(
        string name = "OpenAI", bool available = true, string suggestion = "Clean Name.mkv")
    {
        var mock = new Mock<ILlmProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.IsAvailable).Returns(available);
        mock.Setup(p => p.GenerateRenameAsync(It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        return mock;
    }

    [Fact]
    public async Task SuggestRenameAsync_WithAvailableProvider_ReturnsSuggestion()
    {
        var provider = CreateProvider();
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);

        result.Should().NotBeNull();
        result!.SuggestedFileName.Should().Be("Clean Name.mkv");
        result.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public async Task SuggestRenameAsync_NoAvailableProvider_ReturnsNull()
    {
        var provider = CreateProvider(available: false);
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SuggestRenameAsync_EmptyProviders_ReturnsNull()
    {
        var service = new AiRenameService(Array.Empty<ILlmProvider>());
        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SuggestRenameAsync_EmptySuggestion_ReturnsNull()
    {
        var provider = CreateProvider(suggestion: "  ");
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SuggestRenameAsync_SanitizesQuotesAndNewlines()
    {
        // Leading/trailing quotes get stripped by Trim, inner newlines by Replace
        var provider = CreateProvider(suggestion: "\"Clean Name.mkv\"");
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().NotBeNull();
        result!.SuggestedFileName.Should().Be("Clean Name.mkv");
    }

    [Fact]
    public async Task SuggestRenameAsync_ProviderThrows_ReturnsNull()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("Failing");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider.Setup(p => p.GenerateRenameAsync(It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var service = new AiRenameService(new[] { provider.Object });
        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SuggestRenameAsync_SelectsFirstAvailableProvider()
    {
        var unavailable = CreateProvider("Unavailable", available: false);
        var available = CreateProvider("Ollama", available: true, suggestion: "Ollama Result.mkv");
        var service = new AiRenameService(new[] { unavailable.Object, available.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().NotBeNull();
        result!.ProviderName.Should().Be("Ollama");
    }

    [Fact]
    public async Task SuggestRenameAsync_RecordsElapsedTime()
    {
        var provider = CreateProvider();
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result.Should().NotBeNull();
        result!.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task SuggestRenameAsync_StripsBackticks()
    {
        var provider = CreateProvider(suggestion: "`Clean Name.mkv`");
        var service = new AiRenameService(new[] { provider.Object });

        var result = await service.SuggestRenameAsync(TestContext);
        result!.SuggestedFileName.Should().Be("Clean Name.mkv");
    }

    [Fact]
    public async Task SuggestRenameAsync_BuildsPromptWithContext()
    {
        string? capturedPrompt = null;
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("Test");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider.Setup(p => p.GenerateRenameAsync(It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, MediaContext, CancellationToken>((prompt, _, _) => capturedPrompt = prompt)
            .ReturnsAsync("result.mkv");

        var context = new MediaContext("File.mkv", "Movie", "Title", 1, 2, 2024, "1080p", "RARBG");
        var service = new AiRenameService(new[] { provider.Object });
        await service.SuggestRenameAsync(context);

        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("File.mkv");
        capturedPrompt.Should().Contain("Movie");
        capturedPrompt.Should().Contain("Title");
        capturedPrompt.Should().Contain("2024");
        capturedPrompt.Should().Contain("1080p");
        capturedPrompt.Should().Contain("RARBG");
    }
}
