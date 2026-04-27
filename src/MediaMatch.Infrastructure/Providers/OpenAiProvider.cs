using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// LLM provider backed by the OpenAI REST API (chat completions).
/// </summary>
public sealed class OpenAiProvider : ILlmProvider
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _httpClient;
    private readonly LlmConfiguration _config;
    private readonly ILogger<OpenAiProvider> _logger;
    private int _rateLimitRemaining = int.MaxValue;

    public string Name => "OpenAI";

    public bool IsAvailable =>
        _config.Provider == LlmProviderType.OpenAI &&
        !string.IsNullOrWhiteSpace(_config.OpenAiApiKey);

    public OpenAiProvider(
        HttpClient httpClient,
        LlmConfiguration config,
        ILogger<OpenAiProvider>? logger = null)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger ?? NullLogger<OpenAiProvider>.Instance;
    }

    public async Task<string> GenerateRenameAsync(string prompt, MediaContext context, CancellationToken ct = default)
    {
        if (_rateLimitRemaining <= 0)
        {
            _logger.LogWarning("OpenAI rate limit exhausted, waiting before retry");
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }

        var request = new OpenAiRequest
        {
            Model = _config.OpenAiModel,
            MaxTokens = _config.MaxTokens,
            Messages =
            [
                new OpenAiMessage { Role = "system", Content = _config.SystemPrompt },
                new OpenAiMessage { Role = "user", Content = prompt }
            ]
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.OpenAiApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        // Track rate limit headers
        if (response.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
                _rateLimitRemaining = remaining;
        }

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OpenAiResponse>(responseJson, JsonOptions);

        var content = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("OpenAI returned empty response");
            return string.Empty;
        }

        _logger.LogDebug("OpenAI suggestion: {Suggestion}", content);
        return content;
    }

    #region OpenAI JSON DTOs

    private sealed class OpenAiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiMessage> Messages { get; set; } = [];

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    #endregion
}
