using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Subtitle provider backed by the OpenSubtitles REST API v1.
/// </summary>
public sealed class OpenSubtitlesProvider : ISubtitleProvider
{
    private const string BaseUrl = "https://api.opensubtitles.com/api/v1";

    private readonly MediaMatchHttpClient _http;
    private readonly ApiConfiguration _config;
    private readonly ApiKeySettings _apiKeys;
    private readonly ILogger<OpenSubtitlesProvider> _logger;

    /// <inheritdoc />
    public string Name => "OpenSubtitles";

    /// <summary>Returns true if an OpenSubtitles API key has been configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKeys.OpenSubtitlesApiKey);

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenSubtitlesProvider"/> class.
    /// </summary>
    /// <param name="http">The HTTP client used for OpenSubtitles API requests.</param>
    /// <param name="config">API configuration settings.</param>
    /// <param name="apiKeys">API key settings containing the OpenSubtitles key.</param>
    /// <param name="logger">Logger instance.</param>
    public OpenSubtitlesProvider(
        MediaMatchHttpClient http,
        ApiConfiguration config,
        ApiKeySettings apiKeys,
        ILogger<OpenSubtitlesProvider> logger)
    {
        _http = http;
        _config = config;
        _apiKeys = apiKeys;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubtitleDescriptor>> SearchAsync(
        string query, string language, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("OpenSubtitles API key not configured, skipping subtitle search");
            return [];
        }

        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{BaseUrl}/subtitles?query={encodedQuery}&languages={Uri.EscapeDataString(language)}";

        return await SearchInternalAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubtitleDescriptor>> SearchByHashAsync(
        string movieHash, long fileSize, string language, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("OpenSubtitles API key not configured, skipping hash search");
            return [];
        }

        var url = $"{BaseUrl}/subtitles?moviehash={Uri.EscapeDataString(movieHash)}&languages={Uri.EscapeDataString(language)}";

        return await SearchInternalAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(SubtitleDescriptor subtitle, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OpenSubtitles API key is required to download subtitles.");

        if (string.IsNullOrEmpty(subtitle.DownloadUrl))
            throw new InvalidOperationException("Subtitle has no download URL.");

        _logger.LogInformation("Downloading subtitle {Name} from OpenSubtitles", subtitle.Name);

        // OpenSubtitles v1 download endpoint returns a JSON with a link
        var response = await _http.PostAsync<DownloadRequest, DownloadResponse>(
            $"{BaseUrl}/download",
            new DownloadRequest(int.Parse(subtitle.DownloadUrl, System.Globalization.CultureInfo.InvariantCulture)),
            AuthHeaders(),
            ct).ConfigureAwait(false);

        if (response?.Link is null)
            throw new InvalidOperationException("Failed to obtain download link from OpenSubtitles.");

        // Fetch the actual subtitle file as a stream
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MediaMatch/1.0");
        using var fileResponse = await httpClient.GetAsync(response.Link, ct).ConfigureAwait(false);
        fileResponse.EnsureSuccessStatusCode();
        // Copy to a MemoryStream so the HttpClient can be disposed
        var memStream = new MemoryStream();
        await fileResponse.Content.CopyToAsync(memStream, ct).ConfigureAwait(false);
        memStream.Position = 0;
        return memStream;
    }

    private async Task<IReadOnlyList<SubtitleDescriptor>> SearchInternalAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync<SearchResponse>(url, AuthHeaders(), ct).ConfigureAwait(false);
            if (response?.Data is null)
                return [];

            var results = new List<SubtitleDescriptor>();

            foreach (var item in response.Data)
            {
                var attrs = item.Attributes;
                if (attrs is null) continue;

                var file = attrs.Files?.FirstOrDefault();
                var format = ParseFormat(attrs.Format);

                results.Add(new SubtitleDescriptor(
                    Name: attrs.Release ?? attrs.FeatureDetails?.Title ?? "Unknown",
                    Language: attrs.Language ?? "en",
                    Format: format,
                    ProviderName: "OpenSubtitles",
                    DownloadUrl: file?.FileId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Hash: attrs.MovieHashMatch is true ? "hash-match" : null,
                    Downloads: attrs.DownloadCount));
            }

            _logger.LogDebug("OpenSubtitles returned {Count} results for {Url}", results.Count, url);
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "OpenSubtitles search failed for {Url}", url);
            return [];
        }
    }

    private static SubtitleFormat ParseFormat(string? format) => format?.ToUpperInvariant() switch
    {
        "SRT" => SubtitleFormat.SubRip,
        "ASS" or "SSA" => SubtitleFormat.SubStationAlpha,
        "SUB" => SubtitleFormat.MicroDVD,
        "SMI" or "SAMI" => SubtitleFormat.Sami,
        _ => SubtitleFormat.Unknown
    };

    /// <summary>
    /// Builds the Api-Key header dictionary required by every OpenSubtitles REST API v1 request.
    /// </summary>
    private Dictionary<string, string> AuthHeaders()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Api-Key"] = _apiKeys.OpenSubtitlesApiKey
        };
    }

    // Private DTOs for OpenSubtitles REST API v1

    private sealed record DownloadRequest(
        [property: JsonPropertyName("file_id")] int FileId);

    private sealed class DownloadResponse
    {
        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }

    private sealed class SearchResponse
    {
        [JsonPropertyName("data")]
        public List<SearchItem>? Data { get; set; }
    }

    private sealed class SearchItem
    {
        [JsonPropertyName("attributes")]
        public SubtitleAttributes? Attributes { get; set; }
    }

    private sealed class SubtitleAttributes
    {
        [JsonPropertyName("release")]
        public string? Release { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("download_count")]
        public int? DownloadCount { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("moviehash_match")]
        public bool? MovieHashMatch { get; set; }

        [JsonPropertyName("files")]
        public List<SubtitleFile>? Files { get; set; }

        [JsonPropertyName("feature_details")]
        public FeatureDetails? FeatureDetails { get; set; }
    }

    private sealed class SubtitleFile
    {
        [JsonPropertyName("file_id")]
        public int FileId { get; set; }

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }
    }

    private sealed class FeatureDetails
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("imdb_id")]
        public int? ImdbId { get; set; }
    }
}
