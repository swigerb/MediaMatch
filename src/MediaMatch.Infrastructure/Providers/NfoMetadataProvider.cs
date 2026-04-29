using System.Xml.Linq;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Reads Kodi-style .nfo sidecar files for local metadata before querying online APIs.
/// Supports &lt;movie&gt;, &lt;episodedetails&gt;, and &lt;tvshow&gt; root elements.
/// </summary>
public sealed class NfoMetadataProvider : IMovieProvider, IEpisodeProvider, ILocalMetadataProvider
{
    private readonly ILogger<NfoMetadataProvider> _logger;

    /// <inheritdoc />
    public string Name => "NFO";

    /// <summary>
    /// Initializes a new instance of the <see cref="NfoMetadataProvider"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public NfoMetadataProvider(ILogger<NfoMetadataProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<NfoMetadataProvider>.Instance;
    }

    // ── IMovieProvider ──────────────────────────────────────────

    /// <inheritdoc />
    public Task<IReadOnlyList<Movie>> SearchAsync(string query, int? year = null, CancellationToken ct = default)
    {
        // NFO provider doesn't search by query — it needs a file path context.
        // The MetadataProviderChain calls SearchByFileAsync instead.
        return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
    }

    /// <inheritdoc />
    public Task<MovieInfo> GetMovieInfoAsync(Movie movie, CancellationToken ct = default)
    {
        // Return minimal info from what we already parsed
        return Task.FromResult(new MovieInfo(
            Name: movie.Name,
            Year: movie.Year,
            TmdbId: movie.TmdbId,
            ImdbId: null,
            Overview: null,
            Tagline: null,
            PosterUrl: null,
            Rating: null,
            Runtime: null,
            Certification: null,
            Genres: [],
            Cast: [],
            Crew: []));
    }

    /// <summary>
    /// Search for movie metadata by looking for a .nfo file adjacent to the given file path.
    /// </summary>
    public Task<IReadOnlyList<Movie>> SearchByFileAsync(string filePath, CancellationToken ct = default)
    {
        var nfoPath = FindNfoFile(filePath);
        if (nfoPath is null)
            return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());

        try
        {
            var doc = XDocument.Load(nfoPath);
            var root = doc.Root;

            if (root?.Name.LocalName is "movie")
            {
                var title = root.Element("title")?.Value ?? string.Empty;
                var yearStr = root.Element("year")?.Value;
                int.TryParse(yearStr, out var year);

                var movie = new Movie(
                    Name: title,
                    Year: year,
                    TmdbId: ParseInt(root.Element("tmdbid")?.Value),
                    ImdbId: ParseImdbId(root.Element("id")?.Value));

                _logger.LogDebug("NFO movie match: {Title} ({Year}) from {Path}", title, year, nfoPath);
                return Task.FromResult<IReadOnlyList<Movie>>([movie]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse NFO file: {Path}", nfoPath);
        }

        return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
    }

    /// <summary>
    /// Return enriched MovieInfo from the NFO file adjacent to the given path.
    /// </summary>
    public Task<MovieInfo?> GetMovieInfoByFileAsync(string filePath, CancellationToken ct = default)
    {
        var nfoPath = FindNfoFile(filePath);
        if (nfoPath is null)
            return Task.FromResult<MovieInfo?>(null);

        try
        {
            var doc = XDocument.Load(nfoPath);
            var root = doc.Root;

            if (root?.Name.LocalName is "movie")
            {
                var info = new MovieInfo(
                    Name: root.Element("title")?.Value ?? string.Empty,
                    Year: ParseInt(root.Element("year")?.Value) ?? 0,
                    TmdbId: ParseInt(root.Element("tmdbid")?.Value),
                    ImdbId: root.Element("id")?.Value,
                    Overview: root.Element("plot")?.Value,
                    Tagline: root.Element("tagline")?.Value,
                    PosterUrl: null,
                    Rating: ParseDouble(root.Element("rating")?.Value),
                    Runtime: ParseInt(root.Element("runtime")?.Value),
                    Certification: root.Element("mpaa")?.Value,
                    Genres: root.Elements("genre").Select(g => g.Value).ToList(),
                    Cast: root.Elements("actor")
                        .Select(a => new Person(
                            a.Element("name")?.Value ?? string.Empty,
                            a.Element("role")?.Value,
                            null))
                        .ToList(),
                    Crew: []);

                return Task.FromResult<MovieInfo?>(info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse NFO movie info: {Path}", nfoPath);
        }

        return Task.FromResult<MovieInfo?>(null);
    }

    // ── IEpisodeProvider ────────────────────────────────────────

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Episode>> GetEpisodesAsync(SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Episode>>(Array.Empty<Episode>());
    }

    /// <inheritdoc />
    public Task<SeriesInfo> GetSeriesInfoAsync(SearchResult series, CancellationToken ct = default)
    {
        return Task.FromResult(new SeriesInfo(
            Name: series.Name,
            Id: series.Id.ToString(),
            Overview: null,
            Network: null,
            Status: null,
            Rating: null,
            Runtime: null,
            Genres: []));
    }

    /// <summary>
    /// Search for episode metadata by looking for a .nfo file adjacent to the given file path.
    /// </summary>
    public Task<Episode?> SearchEpisodeByFileAsync(string filePath, CancellationToken ct = default)
    {
        var nfoPath = FindNfoFile(filePath);
        if (nfoPath is null)
            return Task.FromResult<Episode?>(null);

        try
        {
            var doc = XDocument.Load(nfoPath);
            var root = doc.Root;

            if (root?.Name.LocalName is "episodedetails")
            {
                var episode = new Episode(
                    SeriesName: root.Element("showtitle")?.Value ?? string.Empty,
                    Season: ParseInt(root.Element("season")?.Value) ?? 1,
                    EpisodeNumber: ParseInt(root.Element("episode")?.Value) ?? 1,
                    Title: root.Element("title")?.Value ?? string.Empty,
                    AirDate: ParseSimpleDate(root.Element("aired")?.Value));

                _logger.LogDebug("NFO episode match: {Series} S{Season}E{Episode} from {Path}",
                    episode.SeriesName, episode.Season, episode.EpisodeNumber, nfoPath);
                return Task.FromResult<Episode?>(episode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse NFO episode: {Path}", nfoPath);
        }

        return Task.FromResult<Episode?>(null);
    }

    /// <summary>
    /// Read tvshow.nfo from the series root folder.
    /// </summary>
    public Task<SeriesInfo?> GetSeriesInfoByFileAsync(string filePath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is null) return Task.FromResult<SeriesInfo?>(null);

        // Look for tvshow.nfo in current dir or parent dirs
        var tvShowNfo = FindTvShowNfo(dir);
        if (tvShowNfo is null) return Task.FromResult<SeriesInfo?>(null);

        try
        {
            var doc = XDocument.Load(tvShowNfo);
            var root = doc.Root;

            if (root?.Name.LocalName is "tvshow")
            {
                var info = new SeriesInfo(
                    Name: root.Element("title")?.Value ?? string.Empty,
                    Id: root.Element("id")?.Value,
                    Overview: root.Element("plot")?.Value,
                    Network: root.Element("studio")?.Value,
                    Status: root.Element("status")?.Value,
                    Rating: ParseDouble(root.Element("rating")?.Value),
                    Runtime: null,
                    Genres: root.Elements("genre").Select(g => g.Value).ToList(),
                    StartDate: ParseSimpleDate(root.Element("premiered")?.Value));

                return Task.FromResult<SeriesInfo?>(info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tvshow.nfo: {Path}", tvShowNfo);
        }

        return Task.FromResult<SeriesInfo?>(null);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string? FindNfoFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is null) return null;

        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var nfoPath = Path.Combine(dir, baseName + ".nfo");

        return File.Exists(nfoPath) ? nfoPath : null;
    }

    private static string? FindTvShowNfo(string directory)
    {
        // Search current directory and up to 2 parent directories
        var dir = directory;
        for (int i = 0; i < 3; i++)
        {
            var path = Path.Combine(dir, "tvshow.nfo");
            if (File.Exists(path)) return path;

            var parent = Path.GetDirectoryName(dir);
            if (parent is null || parent == dir) break;
            dir = parent;
        }

        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, out var result)) return result;
        return null;
    }

    private static string? ParseImdbId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? value : "tt" + value;
    }

    private static double? ParseDouble(string? value)
    {
        if (double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private static SimpleDate? ParseSimpleDate(string? value)
    {
        if (value is null) return null;
        if (DateOnly.TryParse(value, out var date))
            return new SimpleDate(date.Year, date.Month, date.Day);
        return null;
    }
}
