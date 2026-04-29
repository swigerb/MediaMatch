using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Movie provider backed by The Movie Database (TMDb) API v3.
/// </summary>
public sealed class TmdbMovieProvider : IMovieProvider
{
    private readonly MediaMatchHttpClient _http;
    private readonly MetadataCache _cache;
    private readonly ApiConfiguration _config;
    private readonly ILogger<TmdbMovieProvider> _logger;

    /// <inheritdoc />
    public string Name => "TMDb";

    /// <summary>Initializes a new instance of the <see cref="TmdbMovieProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for TMDb API requests.</param>
    /// <param name="cache">The metadata cache for storing API responses.</param>
    /// <param name="config">The API configuration containing the TMDb API key.</param>
    /// <param name="logger">The logger instance.</param>
    public TmdbMovieProvider(
        MediaMatchHttpClient http,
        MetadataCache cache,
        ApiConfiguration config,
        ILogger<TmdbMovieProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether a TMDb API key has been configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.TmdbApiKey);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Movie>> SearchAsync(string query, int? year = null, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping movie search");
            return Array.Empty<Movie>();
        }

        var cacheKey = $"tmdb:movie:search:{query}:{year}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<Movie>>(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/search/movie?api_key={_config.TmdbApiKey}&query={Uri.EscapeDataString(query)}&language={_config.Language}";
            if (year.HasValue)
                url += $"&year={year.Value}";

            _logger.LogDebug("TMDb movie search: {Query} year={Year}", query, year);

            var response = await _http.GetAsync<TmdbSearchResponse>(url, ct).ConfigureAwait(false);
            if (response?.Results is null)
                return Array.Empty<Movie>();

            return response.Results
                .Select(r => new Movie(
                    Name: r.Title ?? r.OriginalTitle ?? string.Empty,
                    Year: ParseYear(r.ReleaseDate),
                    TmdbId: r.Id,
                    Language: r.OriginalLanguage))
                .ToList()
                .AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MovieInfo> GetMovieInfoAsync(Movie movie, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping movie info lookup");
            return new MovieInfo(
                Name: movie.Name, Year: movie.Year, TmdbId: null, ImdbId: null,
                Overview: null, Tagline: null, PosterUrl: null, Rating: null,
                Runtime: null, Certification: null, Genres: [], Cast: [], Crew: []);
        }

        if (movie.TmdbId is null)
            throw new ArgumentException("Movie must have a TmdbId to fetch details.", nameof(movie));

        var cacheKey = $"tmdb:movie:info:{movie.TmdbId}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/movie/{movie.TmdbId}?api_key={_config.TmdbApiKey}&language={_config.Language}&append_to_response=credits,release_dates";

            _logger.LogDebug("TMDb movie details: {TmdbId}", movie.TmdbId);

            var detail = await _http.GetAsync<TmdbMovieDetail>(url, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"TMDb returned no data for movie {movie.TmdbId}");

            var cast = detail.Credits?.Cast?
                .OrderBy(c => c.Order)
                .Select(c => new Person(
                    Name: c.Name ?? string.Empty,
                    Character: c.Character,
                    TmdbId: c.Id,
                    ProfileUrl: c.ProfilePath is not null ? $"{_config.TmdbImageBaseUrl}{c.ProfilePath}" : null,
                    Order: c.Order))
                .ToList() ?? [];

            var crew = detail.Credits?.Crew?
                .Select(c => new Person(
                    Name: c.Name ?? string.Empty,
                    Department: c.Department,
                    Job: c.Job,
                    TmdbId: c.Id,
                    ProfileUrl: c.ProfilePath is not null ? $"{_config.TmdbImageBaseUrl}{c.ProfilePath}" : null))
                .ToList() ?? [];

            var certification = ExtractCertification(detail.ReleaseDates);

            var posterUrl = detail.PosterPath is not null
                ? $"{_config.TmdbImageBaseUrl}{detail.PosterPath}"
                : null;

            return new MovieInfo(
                Name: detail.Title ?? movie.Name,
                Year: ParseYear(detail.ReleaseDate),
                TmdbId: detail.Id,
                ImdbId: detail.ImdbId,
                Overview: detail.Overview,
                Tagline: detail.Tagline,
                PosterUrl: posterUrl,
                Rating: detail.VoteAverage,
                Runtime: detail.Runtime,
                Certification: certification,
                Genres: detail.Genres?.Select(g => g.Name ?? string.Empty).ToList() ?? [],
                Cast: cast,
                Crew: crew,
                OriginalLanguage: detail.OriginalLanguage,
                OriginalTitle: detail.OriginalTitle,
                Revenue: detail.Revenue,
                Budget: detail.Budget,
                Collection: detail.BelongsToCollection?.Name);
        }).ConfigureAwait(false);
    }

    private static int ParseYear(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return 0;
        return DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var d) ? d.Year : 0;
    }

    private static string? ExtractCertification(TmdbReleaseDatesContainer? container)
    {
        var us = container?.Results?.FirstOrDefault(r =>
            string.Equals(r.Iso3166_1, "US", StringComparison.OrdinalIgnoreCase));
        return us?.ReleaseDates?.FirstOrDefault(rd => !string.IsNullOrEmpty(rd.Certification))?.Certification;
    }

    #region TMDb JSON DTOs

    private sealed class TmdbSearchResponse
    {
        public List<TmdbSearchResult>? Results { get; set; }
    }

    private sealed class TmdbSearchResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
    }

    private sealed class TmdbMovieDetail
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        public string? Tagline { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }
        public int? Runtime { get; set; }
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        public long? Revenue { get; set; }
        public long? Budget { get; set; }
        public List<TmdbGenre>? Genres { get; set; }
        public TmdbCredits? Credits { get; set; }
        [JsonPropertyName("release_dates")]
        public TmdbReleaseDatesContainer? ReleaseDates { get; set; }
        [JsonPropertyName("belongs_to_collection")]
        public TmdbCollection? BelongsToCollection { get; set; }
    }

    private sealed class TmdbGenre { public string? Name { get; set; } }
    private sealed class TmdbCollection { public string? Name { get; set; } }

    private sealed class TmdbCredits
    {
        public List<TmdbCastMember>? Cast { get; set; }
        public List<TmdbCrewMember>? Crew { get; set; }
    }

    private sealed class TmdbCastMember
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Character { get; set; }
        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
        public int? Order { get; set; }
    }

    private sealed class TmdbCrewMember
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Job { get; set; }
        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    private sealed class TmdbReleaseDatesContainer
    {
        public List<TmdbReleaseDateCountry>? Results { get; set; }
    }

    private sealed class TmdbReleaseDateCountry
    {
        [JsonPropertyName("iso_3166_1")]
        public string? Iso3166_1 { get; set; }
        [JsonPropertyName("release_dates")]
        public List<TmdbReleaseDate>? ReleaseDates { get; set; }
    }

    private sealed class TmdbReleaseDate
    {
        public string? Certification { get; set; }
    }

    #endregion
}
