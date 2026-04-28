using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// LLM provider backed by Azure OpenAI Service.
/// Uses api-key header authentication instead of Bearer token.
/// </summary>
public sealed class AzureOpenAiProvider : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _httpClient;
    private readonly LlmConfiguration _config;
    private readonly ILogger<AzureOpenAiProvider> _logger;

    /// <inheritdoc />
    public string Name => "AzureOpenAI";

    /// <inheritdoc />
    public bool IsAvailable =>
        _config.Provider == LlmProviderType.AzureOpenAI &&
        !string.IsNullOrWhiteSpace(_config.AzureOpenAiEndpoint) &&
        !string.IsNullOrWhiteSpace(_config.AzureOpenAiApiKey) &&
        !string.IsNullOrWhiteSpace(_config.AzureOpenAiDeployment);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for Azure OpenAI API requests.</param>
    /// <param name="config">LLM configuration containing endpoint, API key, and deployment settings.</param>
    /// <param name="logger">Optional logger instance.</param>
    public AzureOpenAiProvider(
        HttpClient httpClient,
        LlmConfiguration config,
        ILogger<AzureOpenAiProvider>? logger = null)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger ?? NullLogger<AzureOpenAiProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<string> GenerateRenameAsync(string prompt, MediaContext context, CancellationToken ct = default)
    {
        var endpoint = _config.AzureOpenAiEndpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_config.AzureOpenAiDeployment}/chat/completions?api-version={_config.AzureOpenAiApiVersion}";

        var request = new AzureChatRequest
        {
            MaxTokens = _config.MaxTokens,
            Messages =
            [
                new AzureChatMessage { Role = "system", Content = _config.SystemPrompt },
                new AzureChatMessage { Role = "user", Content = prompt }
            ]
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("api-key", _config.AzureOpenAiApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<AzureChatResponse>(responseJson, JsonOptions);

        var content = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Azure OpenAI returned empty response");
            return string.Empty;
        }

        _logger.LogDebug("Azure OpenAI suggestion: {Suggestion}", content);
        return content;
    }

    #region Azure OpenAI JSON DTOs

    private sealed class AzureChatRequest
    {
        [JsonPropertyName("messages")]
        public List<AzureChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    private sealed class AzureChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AzureChatResponse
    {
        [JsonPropertyName("choices")]
        public List<AzureChatChoice>? Choices { get; set; }
    }

    private sealed class AzureChatChoice
    {
        [JsonPropertyName("message")]
        public AzureChatMessage? Message { get; set; }
    }

    #endregion
}
