using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.EndToEnd.Tests.AiRename;

/// <summary>
/// E2E: AI/LLM rename service — mock LLM provider → request → parse → apply suggestion.
/// </summary>
public sealed class AiRenameServiceE2ETests
{
    private static MediaContext MakeContext(
        string fileName,
        string? type = null,
        string? title = null,
        int? season = null,
        int? episode = null,
        int? year = null,
        string? quality = null,
        string? group = null) =>
        new(fileName, type, title, season, episode, year, quality, group);

    // ── Provider selection ────────────────────────────────────────────────

    [Fact]
    public async Task AiRename_AvailableProvider_ReturnsSuggestion()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("OpenAI");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(),
                It.IsAny<MediaContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Breaking Bad - S01E01 - Pilot.mkv");

        var service = new AiRenameService([provider.Object]);
        var ctx = MakeContext("Breaking.Bad.S01E01.mkv", "TV Series", "Pilot", 1, 1);

        var suggestion = await service.SuggestRenameAsync(ctx);

        suggestion.Should().NotBeNull();
        suggestion!.SuggestedFileName.Should().Be("Breaking Bad - S01E01 - Pilot.mkv");
        suggestion.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public async Task AiRename_NoAvailableProvider_ReturnsNull()
    {
        var unavailableProvider = new Mock<ILlmProvider>();
        unavailableProvider.Setup(p => p.Name).Returns("OpenAI");
        unavailableProvider.Setup(p => p.IsAvailable).Returns(false);

        var service = new AiRenameService([unavailableProvider.Object]);
        var ctx = MakeContext("test.mkv");

        var suggestion = await service.SuggestRenameAsync(ctx);

        suggestion.Should().BeNull();
    }

    [Fact]
    public async Task AiRename_EmptyProviderList_ReturnsNull()
    {
        var service = new AiRenameService([]);
        var ctx = MakeContext("test.mkv");

        var suggestion = await service.SuggestRenameAsync(ctx);

        suggestion.Should().BeNull();
    }

    // ── Sanitization ──────────────────────────────────────────────────────

    [Fact]
    public async Task AiRename_SuggestionWithQuotes_Sanitized()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("OpenAI");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("\"Inception (2010).mkv\"");

        var service = new AiRenameService([provider.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("Inception.2010.mkv", "Movie", "Inception", year: 2010));

        suggestion.Should().NotBeNull();
        suggestion!.SuggestedFileName.Should().Be("Inception (2010).mkv");
        suggestion.SuggestedFileName.Should().NotStartWith("\"");
    }

    [Fact]
    public async Task AiRename_SuggestionWithNewlines_Sanitized()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("Ollama");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Inception (2010).mkv\n\n");

        var service = new AiRenameService([provider.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("Inception.2010.mkv"));

        suggestion.Should().NotBeNull();
        suggestion!.SuggestedFileName.Should().NotContain("\n");
        suggestion.SuggestedFileName.Should().NotContain("\r");
    }

    [Fact]
    public async Task AiRename_EmptySuggestion_ReturnsNull()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("OpenAI");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("  ");  // whitespace only

        var service = new AiRenameService([provider.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("test.mkv"));

        suggestion.Should().BeNull();
    }

    // ── Provider priority ─────────────────────────────────────────────────

    [Fact]
    public async Task AiRename_FirstAvailableProviderSelected()
    {
        var openAi = new Mock<ILlmProvider>();
        openAi.Setup(p => p.Name).Returns("OpenAI");
        openAi.Setup(p => p.IsAvailable).Returns(false);

        var azure = new Mock<ILlmProvider>();
        azure.Setup(p => p.Name).Returns("AzureOpenAI");
        azure.Setup(p => p.IsAvailable).Returns(true);
        azure
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Inception (2010).mkv");

        var ollama = new Mock<ILlmProvider>();
        ollama.Setup(p => p.Name).Returns("Ollama");
        ollama.Setup(p => p.IsAvailable).Returns(true);

        var service = new AiRenameService([openAi.Object, azure.Object, ollama.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("Inception.2010.mkv"));

        suggestion.Should().NotBeNull();
        suggestion!.ProviderName.Should().Be("AzureOpenAI");

        // Ollama should NOT have been called
        ollama.Verify(
            p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Error handling ────────────────────────────────────────────────────

    [Fact]
    public async Task AiRename_ProviderThrows_ReturnsNull()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("OpenAI");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var service = new AiRenameService([provider.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("test.mkv"));

        suggestion.Should().BeNull();
    }

    // ── Elapsed time ──────────────────────────────────────────────────────

    [Fact]
    public async Task AiRename_Suggestion_IncludesElapsedTime()
    {
        var provider = new Mock<ILlmProvider>();
        provider.Setup(p => p.Name).Returns("OpenAI");
        provider.Setup(p => p.IsAvailable).Returns(true);
        provider
            .Setup(p => p.GenerateRenameAsync(
                It.IsAny<string>(), It.IsAny<MediaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Inception (2010).mkv");

        var service = new AiRenameService([provider.Object]);
        var suggestion = await service.SuggestRenameAsync(MakeContext("Inception.2010.mkv"));

        suggestion.Should().NotBeNull();
        suggestion!.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }
}
