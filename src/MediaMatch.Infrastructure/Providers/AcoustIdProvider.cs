using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Music metadata provider backed by the AcoustID fingerprint lookup API.
/// Takes a Chromaprint fingerprint + duration and returns MusicBrainz recording data.
/// </summary>
public sealed class AcoustIdProvider : IMusicProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<AcoustIdProvider> _logger;

    public string Name => "AcoustID";

    public AcoustIdProvider(HttpClient http, ApiKeySettings apiKeys, ILogger<AcoustIdProvider>? logger = null)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("https://api.acoustid.org/v2/");
        _apiKey = apiKeys.AcoustIdApiKey;
        _logger = logger ?? NullLogger<AcoustIdProvider>.Instance;
    }

    public async Task<MusicTrack?> LookupAsync(string fingerprint, int duration, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("AcoustID API key not configured, skipping lookup");
            return null;
        }

        var url = $"lookup?client={Uri.EscapeDataString(_apiKey)}&duration={duration}&fingerprint={Uri.EscapeDataString(fingerprint)}&meta=recordings+releasegroups";

        _logger.LogDebug("AcoustID lookup: duration={Duration}", duration);

        try
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AcoustIdResponse>(JsonOptions, ct);
            if (result?.Results is null or { Count: 0 })
                return null;

            // Take the highest-score result with recordings
            var best = result.Results
                .Where(r => r.Recordings is { Count: > 0 })
                .OrderByDescending(r => r.Score)
                .FirstOrDefault();

            if (best?.Recordings is null or { Count: 0 })
                return null;

            var recording = best.Recordings[0];
            var artist = recording.Artists?.FirstOrDefault()?.Name;

            int? year = null;
            string? album = null;
            if (recording.ReleaseGroups is { Count: > 0 })
            {
                var rg = recording.ReleaseGroups[0];
                album = rg.Title;
                if (rg.FirstReleaseDate is { Length: >= 4 } dateStr && int.TryParse(dateStr[..4], out var y))
                    year = y;
            }

            return new MusicTrack(
                Title: recording.Title ?? string.Empty,
                Artist: artist ?? string.Empty,
                Album: album,
                MusicBrainzId: recording.Id,
                Year: year,
                Duration: duration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AcoustID lookup failed");
            return null;
        }
    }

    public Task<IReadOnlyList<MusicTrack>> SearchAsync(string artist, string title, CancellationToken ct = default)
    {
        // AcoustID is fingerprint-only; search is not supported.
        return Task.FromResult<IReadOnlyList<MusicTrack>>(Array.Empty<MusicTrack>());
    }

    // ── Private DTOs ────────────────────────────────────────────

    private sealed class AcoustIdResponse
    {
        [JsonPropertyName("results")]
        public List<AcoustIdResult>? Results { get; set; }
    }

    private sealed class AcoustIdResult
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("recordings")]
        public List<AcoustIdRecording>? Recordings { get; set; }
    }

    private sealed class AcoustIdRecording
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("artists")]
        public List<AcoustIdArtist>? Artists { get; set; }

        [JsonPropertyName("releasegroups")]
        public List<AcoustIdReleaseGroup>? ReleaseGroups { get; set; }
    }

    private sealed class AcoustIdArtist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class AcoustIdReleaseGroup
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("firstreleasedate")]
        public string? FirstReleaseDate { get; set; }
    }
}
