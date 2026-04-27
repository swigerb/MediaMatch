namespace MediaMatch.Core.Configuration;

/// <summary>
/// Configuration for the AI-assisted renaming LLM integration.
/// </summary>
public sealed class LlmConfiguration
{
    /// <summary>Which LLM provider to use.</summary>
    public LlmProviderType Provider { get; set; } = LlmProviderType.None;

    /// <summary>OpenAI API key (used by OpenAI provider).</summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>OpenAI model name.</summary>
    public string OpenAiModel { get; set; } = "gpt-4o";

    /// <summary>Azure OpenAI endpoint URL (e.g. https://myresource.openai.azure.com/).</summary>
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;

    /// <summary>Azure OpenAI API key.</summary>
    public string AzureOpenAiApiKey { get; set; } = string.Empty;

    /// <summary>Azure OpenAI deployment name.</summary>
    public string AzureOpenAiDeployment { get; set; } = string.Empty;

    /// <summary>Azure OpenAI API version.</summary>
    public string AzureOpenAiApiVersion { get; set; } = "2024-02-01";

    /// <summary>Ollama endpoint URL.</summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>Ollama model name.</summary>
    public string OllamaModel { get; set; } = "llama3";

    /// <summary>Custom system prompt for the LLM.</summary>
    public string SystemPrompt { get; set; } =
        "You are a media file renaming assistant. Given information about a media file, suggest a clean, properly formatted filename. " +
        "Return ONLY the suggested filename with extension, nothing else.";

    /// <summary>HTTP timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum tokens for the LLM response.</summary>
    public int MaxTokens { get; set; } = 500;
}

/// <summary>
/// Available LLM provider types.
/// </summary>
public enum LlmProviderType
{
    None = 0,
    OpenAI = 1,
    AzureOpenAI = 2,
    Ollama = 3
}
