using MediaMatch.Core.Expressions;
using MediaMatch.Core.Models;
using Scriban.Runtime;

namespace MediaMatch.Application.Expressions;

/// <summary>
/// Exposes media metadata as Scriban template variables.
/// Maps FileBot-style bindings to Scriban ScriptObject properties.
/// </summary>
public class MediaBindings : ScriptObject, IMediaBindings
{
    private MediaBindings() { }

    /// <summary>Create bindings for an episode file.</summary>
    public static MediaBindings ForEpisode(Episode episode, SeriesInfo? seriesInfo = null, string? filePath = null, MediaTechnicalInfo? techInfo = null, int? endEpisode = null)
    {
        var b = new MediaBindings();

        b.SetValue("n", episode.SeriesName, readOnly: false);
        b.SetValue("s", episode.Season, readOnly: false);
        b.SetValue("e", episode.EpisodeNumber, readOnly: false);
        b.SetValue("s00", episode.Season.ToString("D2"), readOnly: false);
        b.SetValue("e00", episode.EpisodeNumber.ToString("D2"), readOnly: false);
        b.SetValue("s00e00", $"S{episode.Season:D2}E{episode.EpisodeNumber:D2}", readOnly: false);
        b.SetValue("sxe", $"{episode.Season}x{episode.EpisodeNumber:D2}", readOnly: false);
        b.SetValue("t", episode.Title, readOnly: false);
        b.SetValue("airdate", episode.AirDate?.ToString(), readOnly: false);
        b.SetValue("absolute", episode.AbsoluteNumber, readOnly: false);

        // Multi-episode bindings
        b.SetValue("startEpisode", episode.EpisodeNumber, readOnly: false);
        b.SetValue("endEpisode", endEpisode ?? episode.EpisodeNumber, readOnly: false);
        b.SetValue("isMultiEpisode", endEpisode.HasValue && endEpisode.Value != episode.EpisodeNumber, readOnly: false);

        if (endEpisode.HasValue && endEpisode.Value != episode.EpisodeNumber)
        {
            // Multi-episode s00e00 ranges
            b.SetValue("s00e00", $"S{episode.Season:D2}E{episode.EpisodeNumber:D2}-E{endEpisode.Value:D2}", readOnly: false);
            b.SetValue("sxe", $"{episode.Season}x{episode.EpisodeNumber:D2}-{endEpisode.Value:D2}", readOnly: false);
        }

        if (seriesInfo is not null)
        {
            b.SetValue("series", seriesInfo, readOnly: false);
            b.SetValue("y", seriesInfo.StartDate?.Year, readOnly: false);
        }

        SetFileBindings(b, filePath);
        SetTechnicalBindings(b, techInfo);
        SetJellyfinBinding(b, episode, seriesInfo, endEpisode);
        return b;
    }

    /// <summary>Create bindings for a movie file.</summary>
    public static MediaBindings ForMovie(Movie movie, MovieInfo? movieInfo = null, string? filePath = null, MediaTechnicalInfo? techInfo = null)
    {
        var b = new MediaBindings();

        b.SetValue("n", movie.Name, readOnly: false);
        b.SetValue("y", movie.Year, readOnly: false);
        b.SetValue("t", movie.Name, readOnly: false);

        if (movieInfo is not null)
        {
            b.SetValue("genre", movieInfo.Genres.Count > 0 ? movieInfo.Genres[0] : null, readOnly: false);
            b.SetValue("genres", movieInfo.Genres, readOnly: false);
            b.SetValue("rating", movieInfo.Rating, readOnly: false);
            b.SetValue("director", FirstCrewByJob(movieInfo.Crew, "Director"), readOnly: false);
            b.SetValue("actors", movieInfo.Cast.Select(p => p.Name).ToList(), readOnly: false);
            b.SetValue("certification", movieInfo.Certification, readOnly: false);
            b.SetValue("imdb", movieInfo.ImdbId, readOnly: false);
            b.SetValue("tmdb", movieInfo.TmdbId, readOnly: false);
        }

        SetFileBindings(b, filePath);
        SetTechnicalBindings(b, techInfo);
        SetJellyfinBinding(b, movie);
        return b;
    }

    // ── IMediaBindings ──────────────────────────────────────────

    public object? GetValue(string name)
    {
        TryGetValue(null!, new Scriban.Parsing.SourceSpan(), name, out var value);
        return value;
    }

    public IReadOnlyDictionary<string, object?> GetAllBindings()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var member in GetMembers())
        {
            TryGetValue(null!, new Scriban.Parsing.SourceSpan(), member, out var value);
            dict[member] = value;
        }
        return dict;
    }

    public bool HasBinding(string name) => Contains(name);

    // ── helpers ─────────────────────────────────────────────────

    private static void SetFileBindings(MediaBindings b, string? filePath)
    {
        if (filePath is not null)
        {
            b.SetValue("fn", Path.GetFileNameWithoutExtension(filePath), readOnly: false);
            b.SetValue("ext", Path.GetExtension(filePath), readOnly: false);
            b.SetValue("extension", Path.GetExtension(filePath), readOnly: false);
            b.SetValue("file", filePath, readOnly: false);
        }
    }

    private static string? FirstCrewByJob(IReadOnlyList<Person> crew, string job) =>
        crew.FirstOrDefault(p => string.Equals(p.Job, job, StringComparison.OrdinalIgnoreCase))?.Name;

    private static void SetTechnicalBindings(MediaBindings b, MediaTechnicalInfo? techInfo)
    {
        if (techInfo is null) return;

        b.SetValue("acf", techInfo.AudioChannels, readOnly: false);
        b.SetValue("dovi", techInfo.DolbyVision, readOnly: false);
        b.SetValue("hdr", techInfo.HdrFormat, readOnly: false);
        b.SetValue("resolution", techInfo.Resolution, readOnly: false);
        b.SetValue("bitdepth", techInfo.BitDepth, readOnly: false);
    }

    private static void SetJellyfinBinding(MediaBindings b, Episode episode, SeriesInfo? seriesInfo, int? endEpisode = null)
    {
        string jellyfinName;
        if (endEpisode.HasValue && endEpisode.Value != episode.EpisodeNumber)
        {
            // Jellyfin multi-episode: SeriesName S01E01-S01E02
            jellyfinName = $"{episode.SeriesName} S{episode.Season:D2}E{episode.EpisodeNumber:D2}-S{episode.Season:D2}E{endEpisode.Value:D2}";
        }
        else
        {
            // Jellyfin naming: SeriesName - SXXEXX - Title
            jellyfinName = $"{episode.SeriesName} - S{episode.Season:D2}E{episode.EpisodeNumber:D2}";
            if (!string.IsNullOrWhiteSpace(episode.Title))
                jellyfinName += $" - {episode.Title}";
        }
        b.SetValue("jellyfin", jellyfinName, readOnly: false);
    }

    private static void SetJellyfinBinding(MediaBindings b, Movie movie)
    {
        // Jellyfin naming: MovieName (Year)/MovieName (Year)
        var jellyfinName = $"{movie.Name} ({movie.Year})";
        b.SetValue("jellyfin", jellyfinName, readOnly: false);
    }

    /// <summary>Create bindings for a music file.</summary>
    public static MediaBindings ForMusic(MusicTrack track, string? filePath = null)
    {
        var b = new MediaBindings();

        b.SetValue("artist", track.Artist, readOnly: false);
        b.SetValue("album", track.Album, readOnly: false);
        b.SetValue("albumartist", track.AlbumArtist ?? track.Artist, readOnly: false);
        b.SetValue("title", track.Title, readOnly: false);
        b.SetValue("t", track.Title, readOnly: false);
        b.SetValue("n", track.Artist, readOnly: false);
        b.SetValue("track", track.TrackNumber, readOnly: false);
        b.SetValue("disc", track.DiscNumber, readOnly: false);
        b.SetValue("genre", track.Genre, readOnly: false);
        b.SetValue("y", track.Year, readOnly: false);
        b.SetValue("featuring", track.DisplayArtist != track.Artist ? string.Join(", ", track.FeaturedArtists ?? []) : null, readOnly: false);

        SetFileBindings(b, filePath);
        return b;
    }
}
