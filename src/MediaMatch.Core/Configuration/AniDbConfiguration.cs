namespace MediaMatch.Core.Configuration;

/// <summary>
/// Configuration for AniDB HTTP API and TVDb mapping integration.
/// </summary>
public sealed class AniDbConfiguration
{
    /// <summary>AniDB HTTP API base URL.</summary>
    public string BaseUrl { get; set; } = "http://api.anidb.net:9001/httpapi";

    /// <summary>AniDB client name (registered with AniDB).</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>AniDB client version.</summary>
    public int ClientVersion { get; set; } = 1;

    /// <summary>Protocol version for the HTTP API.</summary>
    public int ProtocolVersion { get; set; } = 1;

    /// <summary>
    /// Minimum interval between AniDB requests in milliseconds.
    /// AniDB enforces ≤1 request per 2 seconds.
    /// </summary>
    public int RateLimitIntervalMs { get; set; } = 2100;

    /// <summary>
    /// URL for the AniDB-to-TVDb mapping XML file.
    /// </summary>
    public string TvdbMappingUrl { get; set; } = "https://raw.githubusercontent.com/Anime-Lists/anime-lists/master/anime-list.xml";

    /// <summary>
    /// How long to cache the AniDB-TVDb mapping file in hours.
    /// </summary>
    public int MappingCacheHours { get; set; } = 24;

    /// <summary>HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    public int MaxRetries { get; set; } = 3;
}
