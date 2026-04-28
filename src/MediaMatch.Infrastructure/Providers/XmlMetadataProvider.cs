using System.Xml.Linq;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Reads Plex/Jellyfin-style .xml sidecar files for local metadata.
/// Searches for {filename}.xml adjacent to the video file.
/// </summary>
public sealed class XmlMetadataProvider : IMovieProvider, IEpisodeProvider, ILocalMetadataProvider
{
    private readonly ILogger<XmlMetadataProvider> _logger;

    /// <inheritdoc />
    public string Name => "XML";

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlMetadataProvider"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public XmlMetadataProvider(ILogger<XmlMetadataProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<XmlMetadataProvider>.Instance;
    }

    // ── IMovieProvider ──────────────────────────────────────────

    /// <inheritdoc />
    public Task<IReadOnlyList<Movie>> SearchAsync(string query, int? year = null, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
    }

    /// <inheritdoc />
    public Task<MovieInfo> GetMovieInfoAsync(Movie movie, CancellationToken ct = default)
    {
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
    /// Search for movie metadata from an XML sidecar file adjacent to the given file path.
    /// </summary>
    public Task<IReadOnlyList<Movie>> SearchByFileAsync(string filePath, CancellationToken ct = default)
    {
        var xmlPath = FindXmlFile(filePath);
        if (xmlPath is null)
            return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());

        try
        {
            var doc = XDocument.Load(xmlPath);
            var root = doc.Root;

            // Plex/Jellyfin XML may use <MediaContainer>, <Item>, or <Movie> as root
            var mediaElement = root?.Name.LocalName switch
            {
                "Movie" or "movie" => root,
                "MediaContainer" => root.Element("Video") ?? root.Element("Movie"),
                "Item" => root,
                _ => root
            };

            if (mediaElement is null)
                return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());

            var title = mediaElement.Attribute("title")?.Value
                ?? mediaElement.Element("Title")?.Value
                ?? mediaElement.Element("title")?.Value
                ?? string.Empty;

            var yearStr = mediaElement.Attribute("year")?.Value
                ?? mediaElement.Element("Year")?.Value
                ?? mediaElement.Element("year")?.Value;

            int.TryParse(yearStr, out var year);

            var movie = new Movie(Name: title, Year: year);
            _logger.LogDebug("XML movie match: {Title} ({Year}) from {Path}", title, year, xmlPath);
            return Task.FromResult<IReadOnlyList<Movie>>(new[] { movie });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML sidecar: {Path}", xmlPath);
        }

        return Task.FromResult<IReadOnlyList<Movie>>(Array.Empty<Movie>());
    }

    /// <summary>
    /// Return enriched MovieInfo from the XML sidecar file adjacent to the given path.
    /// </summary>
    public Task<MovieInfo?> GetMovieInfoByFileAsync(string filePath, CancellationToken ct = default)
    {
        var xmlPath = FindXmlFile(filePath);
        if (xmlPath is null)
            return Task.FromResult<MovieInfo?>(null);

        try
        {
            var doc = XDocument.Load(xmlPath);
            var root = doc.Root;
            var el = root?.Name.LocalName switch
            {
                "Movie" or "movie" => root,
                "MediaContainer" => root.Element("Video") ?? root.Element("Movie"),
                "Item" => root,
                _ => root
            };

            if (el is null) return Task.FromResult<MovieInfo?>(null);

            var info = new MovieInfo(
                Name: GetAttrOrElement(el, "title") ?? string.Empty,
                Year: ParseInt(GetAttrOrElement(el, "year")) ?? 0,
                TmdbId: null,
                ImdbId: null,
                Overview: GetAttrOrElement(el, "summary") ?? GetAttrOrElement(el, "plot"),
                Tagline: GetAttrOrElement(el, "tagline"),
                PosterUrl: null,
                Rating: ParseDouble(GetAttrOrElement(el, "rating")),
                Runtime: ParseInt(GetAttrOrElement(el, "duration")),
                Certification: GetAttrOrElement(el, "contentRating"),
                Genres: el.Elements("Genre")
                    .Concat(el.Elements("genre"))
                    .Select(g => g.Attribute("tag")?.Value ?? g.Value)
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .ToList(),
                Cast: [],
                Crew: []);

            return Task.FromResult<MovieInfo?>(info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML movie info: {Path}", xmlPath);
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
    /// Search for episode metadata from an XML sidecar file adjacent to the given file path.
    /// </summary>
    public Task<Episode?> SearchEpisodeByFileAsync(string filePath, CancellationToken ct = default)
    {
        var xmlPath = FindXmlFile(filePath);
        if (xmlPath is null)
            return Task.FromResult<Episode?>(null);

        try
        {
            var doc = XDocument.Load(xmlPath);
            var root = doc.Root;

            var el = root?.Name.LocalName switch
            {
                "Episode" or "episodedetails" => root,
                "MediaContainer" => root.Element("Video") ?? root.Element("Episode"),
                "Item" => root,
                _ => root
            };

            if (el is null) return Task.FromResult<Episode?>(null);

            var seriesName = GetAttrOrElement(el, "grandparentTitle")
                ?? GetAttrOrElement(el, "showtitle")
                ?? string.Empty;
            var season = ParseInt(GetAttrOrElement(el, "parentIndex") ?? GetAttrOrElement(el, "season")) ?? 1;
            var epNum = ParseInt(GetAttrOrElement(el, "index") ?? GetAttrOrElement(el, "episode")) ?? 1;
            var title = GetAttrOrElement(el, "title") ?? string.Empty;

            var episode = new Episode(
                SeriesName: seriesName,
                Season: season,
                EpisodeNumber: epNum,
                Title: title,
                AirDate: ParseSimpleDate(GetAttrOrElement(el, "originallyAvailableAt")
                    ?? GetAttrOrElement(el, "aired")));

            _logger.LogDebug("XML episode match: {Series} S{Season}E{Episode} from {Path}",
                seriesName, season, epNum, xmlPath);
            return Task.FromResult<Episode?>(episode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML episode: {Path}", xmlPath);
        }

        return Task.FromResult<Episode?>(null);
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Get series info from an XML sidecar file. Returns null — XML sidecars
    /// typically don't have a separate series-level file like tvshow.nfo.
    /// </summary>
    public Task<SeriesInfo?> GetSeriesInfoByFileAsync(string filePath, CancellationToken ct = default)
    {
        return Task.FromResult<SeriesInfo?>(null);
    }

    // ── File lookup ─────────────────────────────────────────────

    private static string? FindXmlFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is null) return null;

        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var xmlPath = Path.Combine(dir, baseName + ".xml");

        return File.Exists(xmlPath) ? xmlPath : null;
    }

    private static string? GetAttrOrElement(XElement el, string name)
    {
        return el.Attribute(name)?.Value
            ?? el.Element(name)?.Value
            ?? el.Element(char.ToUpperInvariant(name[0]) + name[1..])?.Value;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, out var result)) return result;
        return null;
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
