using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Music metadata provider backed by the MusicBrainz REST API.
/// Rate limited to 1 request/second with User-Agent identification.
/// </summary>
public sealed class MusicBrainzProvider : IMusicProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<MusicBrainzProvider> _logger;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <summary>Initializes a new instance of the <see cref="MusicBrainzProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for MusicBrainz API requests.</param>
    /// <param name="logger">The logger instance.</param>
    public MusicBrainzProvider(HttpClient http, ILogger<MusicBrainzProvider>? logger = null)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("https://musicbrainz.org/ws/2/");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaMatch/0.1.0 (https://github.com/swigerb/MediaMatch)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _logger = logger ?? NullLogger<MusicBrainzProvider>.Instance;
    }

    /// <inheritdoc />
    public Task<MusicTrack?> LookupAsync(string fingerprint, int duration, CancellationToken ct = default)
    {
        // MusicBrainz does not support fingerprint lookup directly — use AcoustID instead.
        return Task.FromResult<MusicTrack?>(null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicTrack>> SearchAsync(string artist, string title, CancellationToken ct = default)
    {
        await RateLimitAsync(ct).ConfigureAwait(false);

        var query = Uri.EscapeDataString($"artist:\"{artist}\" AND recording:\"{title}\"");
        var url = $"recording?query={query}&fmt=json&limit=5";

        _logger.LogDebug("MusicBrainz search: artist={Artist} title={Title}", artist, title);

        try
        {
            var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MbRecordingSearchResponse>(JsonOptions, ct).ConfigureAwait(false);
            if (result?.Recordings is null or { Count: 0 })
                return Array.Empty<MusicTrack>();

            return result.Recordings
                .Select(MapToMusicTrack)
                .Where(t => t is not null)
                .Cast<MusicTrack>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MusicBrainz search failed for {Artist} - {Title}", artist, title);
            return Array.Empty<MusicTrack>();
        }
    }

    private static MusicTrack? MapToMusicTrack(MbRecording recording)
    {
        var artist = recording.ArtistCredit?.FirstOrDefault()?.Artist?.Name;
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(recording.Title))
            return null;

        var release = recording.Releases?.FirstOrDefault();
        int? year = null;
        if (release?.Date is { Length: >= 4 } dateStr && int.TryParse(dateStr[..4], out var y))
            year = y;

        int? trackNumber = null;
        int? discNumber = null;
        if (release?.Media is { Count: > 0 })
        {
            var media = release.Media[0];
            discNumber = media.Position;
            var track = media.Track?.FirstOrDefault();
            if (track?.Position is not null)
                trackNumber = track.Position;
        }

        return new MusicTrack(
            Title: recording.Title,
            Artist: artist,
            Album: release?.Title,
            AlbumArtist: recording.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
            TrackNumber: trackNumber,
            DiscNumber: discNumber,
            Genre: null,
            Year: year,
            MusicBrainzId: recording.Id,
            Duration: recording.Length.HasValue ? recording.Length.Value / 1000 : null);
    }

    private async Task RateLimitAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequest;
            if (elapsed.TotalMilliseconds < 1000)
                await Task.Delay(1000 - (int)elapsed.TotalMilliseconds, ct).ConfigureAwait(false);
            _lastRequest = DateTimeOffset.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    // ── Private DTOs ────────────────────────────────────────────

    private sealed class MbRecordingSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MbRecording>? Recordings { get; set; }
    }

    private sealed class MbRecording
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("artist-credit")]
        public List<MbArtistCredit>? ArtistCredit { get; set; }

        [JsonPropertyName("releases")]
        public List<MbRelease>? Releases { get; set; }
    }

    private sealed class MbArtistCredit
    {
        [JsonPropertyName("artist")]
        public MbArtist? Artist { get; set; }
    }

    private sealed class MbArtist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class MbRelease
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("media")]
        public List<MbMedia>? Media { get; set; }
    }

    private sealed class MbMedia
    {
        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonPropertyName("track")]
        public List<MbTrack>? Track { get; set; }
    }

    private sealed class MbTrack
    {
        [JsonPropertyName("position")]
        public int? Position { get; set; }
    }
}
