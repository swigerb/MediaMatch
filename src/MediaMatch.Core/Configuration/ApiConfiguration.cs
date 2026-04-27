namespace MediaMatch.Core.Configuration;

/// <summary>
/// Configuration settings for external metadata API providers.
/// </summary>
public sealed class ApiConfiguration
{
    /// <summary>TMDb API key (v3 auth).</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>TMDb API base URL.</summary>
    public string TmdbBaseUrl { get; set; } = "https://api.themoviedb.org/3";

    /// <summary>TMDb image base URL.</summary>
    public string TmdbImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/original";

    /// <summary>TVDb API key (v4 auth).</summary>
    public string TvdbApiKey { get; set; } = string.Empty;

    /// <summary>TVDb API base URL (v4).</summary>
    public string TvdbBaseUrl { get; set; } = "https://api4.thetvdb.com/v4";

    /// <summary>Default HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Metadata cache TTL in minutes.</summary>
    public int CacheTtlMinutes { get; set; } = 60;

    /// <summary>Preferred language code (e.g. "en-US").</summary>
    public string Language { get; set; } = "en-US";
}
