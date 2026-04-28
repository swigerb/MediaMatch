using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// LLM provider backed by a local Ollama instance.
/// No API key needed — connects to http://localhost:11434 by default.
/// </summary>
public sealed class OllamaProvider : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _httpClient;
    private readonly LlmConfiguration _config;
    private readonly ILogger<OllamaProvider> _logger;

    /// <inheritdoc />
    public string Name => "Ollama";

    /// <inheritdoc />
    public bool IsAvailable => _config.Provider == LlmProviderType.Ollama;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for Ollama API requests.</param>
    /// <param name="config">LLM configuration containing endpoint and model settings.</param>
    /// <param name="logger">Optional logger instance.</param>
    public OllamaProvider(
        HttpClient httpClient,
        LlmConfiguration config,
        ILogger<OllamaProvider>? logger = null)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger ?? NullLogger<OllamaProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<string> GenerateRenameAsync(string prompt, MediaContext context, CancellationToken ct = default)
    {
        var endpoint = _config.OllamaEndpoint.TrimEnd('/');
        var url = $"{endpoint}/api/generate";

        var fullPrompt = $"{_config.SystemPrompt}\n\n{prompt}";
        var request = new OllamaRequest
        {
            Model = _config.OllamaModel,
            Prompt = fullPrompt,
            Stream = false
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, JsonOptions);

        var content = result?.Response?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Ollama returned empty response");
            return string.Empty;
        }

        _logger.LogDebug("Ollama suggestion: {Suggestion}", content);
        return content;
    }

    #region Ollama JSON DTOs

    private sealed class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    #endregion
}
