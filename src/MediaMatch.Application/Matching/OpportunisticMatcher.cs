using System.Diagnostics;
using MediaMatch.Application.Detection;
using MediaMatch.Application.Matching;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Matching;

/// <summary>
/// Triggered when strict matching (≥0.85 confidence) fails.
/// Relaxes the threshold to 0.60 and returns up to 5 ranked candidates.
/// </summary>
public sealed class OpportunisticMatcher
{
    private static readonly ActivitySource Activity = new("MediaMatch", "0.1.0");

    private const float OpportunisticThreshold = 0.60f;
    private const int MaxSuggestions = 5;

    private readonly IReadOnlyList<IEpisodeProvider> _episodeProviders;
    private readonly IReadOnlyList<IMovieProvider> _movieProviders;
    private readonly EpisodeMatcher _episodeMatcher;
    private readonly ILogger<OpportunisticMatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpportunisticMatcher"/> class.
    /// </summary>
    /// <param name="episodeProviders">The episode metadata providers to query.</param>
    /// <param name="movieProviders">The movie metadata providers to query.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public OpportunisticMatcher(
        IEnumerable<IEpisodeProvider> episodeProviders,
        IEnumerable<IMovieProvider> movieProviders,
        ILogger<OpportunisticMatcher>? logger = null)
    {
        _episodeProviders = episodeProviders.ToList();
        _movieProviders = movieProviders.ToList();
        _episodeMatcher = new EpisodeMatcher();
        _logger = logger ?? NullLogger<OpportunisticMatcher>.Instance;
    }

    /// <summary>
    /// Queries all available providers and returns up to 5 suggestions
    /// ranked by confidence, using the relaxed 0.60 threshold.
    /// </summary>
    public async Task<IReadOnlyList<MatchSuggestion>> SuggestAsync(
        string filePath,
        DetectionResult detection,
        CancellationToken ct = default)
    {
        using var activity = Activity.StartActivity("mediamatch.match.opportunistic");
        activity?.SetTag("mediamatch.file_name", Path.GetFileName(filePath));
        activity?.SetTag("mediamatch.media_type", detection.MediaType.ToString());

        _logger.LogInformation("Opportunistic matching for {FileName} (type={MediaType})",
            Path.GetFileName(filePath), detection.MediaType);

        var suggestions = detection.MediaType switch
        {
            MediaType.TvSeries or MediaType.Anime =>
                await SuggestEpisodesAsync(filePath, detection, ct).ConfigureAwait(false),
            MediaType.Movie =>
                await SuggestMoviesAsync(filePath, detection, ct).ConfigureAwait(false),
            _ => Array.Empty<MatchSuggestion>()
        };

        activity?.SetTag("mediamatch.suggestion_count", suggestions.Count);
        return suggestions;
    }

    private async Task<IReadOnlyList<MatchSuggestion>> SuggestEpisodesAsync(
        string filePath, DetectionResult detection, CancellationToken ct)
    {
        var releaseInfo = detection.ReleaseInfo;
        var searchQuery = releaseInfo.CleanTitle;

        if (string.IsNullOrWhiteSpace(searchQuery))
            return Array.Empty<MatchSuggestion>();

        var allSuggestions = new List<MatchSuggestion>();

        foreach (var provider in _episodeProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var searchResults = await provider.SearchAsync(searchQuery, ct).ConfigureAwait(false);

                foreach (var series in searchResults.Take(3))
                {
                    var episodes = await provider.GetEpisodesAsync(series, ct: ct).ConfigureAwait(false);
                    var matches = _episodeMatcher.MatchFiles([filePath], episodes);

                    if (matches.Count > 0)
                    {
                        var match = matches[0];
                        var confidence = (double)(match.Score * detection.Confidence);

                        if (confidence >= OpportunisticThreshold)
                        {
                            SeriesInfo? seriesInfo = null;
                            try
                            {
                                seriesInfo = await provider.GetSeriesInfoAsync(series, ct).ConfigureAwait(false);
                            }
                            catch
                            {
                                // Non-critical — proceed without metadata summary
                            }

                            var summary = BuildEpisodeSummary(match.Candidate, seriesInfo);

                            allSuggestions.Add(new MatchSuggestion(
                                ProviderName: provider.Name,
                                Confidence: confidence,
                                Title: series.Name,
                                Year: seriesInfo?.StartDate?.Year,
                                MetadataSummary: summary,
                                ProviderId: series.Id.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Opportunistic episode provider {Provider} failed for '{Query}'",
                    provider.Name, searchQuery);
            }
        }

        return allSuggestions
            .OrderByDescending(s => s.Confidence)
            .Take(MaxSuggestions)
            .ToList()
            .AsReadOnly();
    }

    private async Task<IReadOnlyList<MatchSuggestion>> SuggestMoviesAsync(
        string filePath, DetectionResult detection, CancellationToken ct)
    {
        var releaseInfo = detection.ReleaseInfo;
        var searchQuery = releaseInfo.CleanTitle;

        if (string.IsNullOrWhiteSpace(searchQuery))
            return Array.Empty<MatchSuggestion>();

        var allSuggestions = new List<MatchSuggestion>();

        foreach (var provider in _movieProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var movies = await provider.SearchAsync(searchQuery, releaseInfo.Year, ct).ConfigureAwait(false);

                foreach (var movie in movies.Take(5))
                {
                    var confidence = (double)ComputeMovieConfidence(detection, movie);

                    if (confidence >= OpportunisticThreshold)
                    {
                        MovieInfo? movieInfo = null;
                        try
                        {
                            movieInfo = await provider.GetMovieInfoAsync(movie, ct).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Non-critical
                        }

                        var summary = BuildMovieSummary(movie, movieInfo);

                        allSuggestions.Add(new MatchSuggestion(
                            ProviderName: provider.Name,
                            Confidence: confidence,
                            Title: movie.Name,
                            Year: movie.Year,
                            MetadataSummary: summary,
                            ProviderId: movie.TmdbId?.ToString() ?? movie.ImdbId));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Opportunistic movie provider {Provider} failed for '{Query}'",
                    provider.Name, searchQuery);
            }
        }

        return allSuggestions
            .OrderByDescending(s => s.Confidence)
            .Take(MaxSuggestions)
            .ToList()
            .AsReadOnly();
    }

    private static float ComputeMovieConfidence(DetectionResult detection, Movie movie)
    {
        float confidence = detection.Confidence;
        var releaseInfo = detection.ReleaseInfo;

        if (releaseInfo.Year.HasValue && releaseInfo.Year.Value == movie.Year)
            confidence = Math.Min(confidence + 0.15f, 1.0f);

        var cleanDetected = Normalization.NormalizeName(releaseInfo.CleanTitle);
        var cleanMovie = Normalization.NormalizeName(movie.Name);
        if (string.Equals(cleanDetected, cleanMovie, StringComparison.OrdinalIgnoreCase))
            confidence = Math.Min(confidence + 0.2f, 1.0f);

        return confidence;
    }

    private static string BuildEpisodeSummary(Episode ep, SeriesInfo? info)
    {
        var parts = new List<string>
        {
            $"S{ep.Season:D2}E{ep.EpisodeNumber:D2}",
            ep.Title
        };

        if (info?.Genres.Count > 0)
            parts.Add(string.Join(", ", info.Genres.Take(3)));

        if (info?.Rating.HasValue == true)
            parts.Add($"Rating: {info.Rating:F1}");

        return string.Join(" | ", parts);
    }

    private static string BuildMovieSummary(Movie movie, MovieInfo? info)
    {
        var parts = new List<string> { $"{movie.Name} ({movie.Year})" };

        if (info?.Genres.Count > 0)
            parts.Add(string.Join(", ", info.Genres.Take(3)));

        if (info?.Rating.HasValue == true)
            parts.Add($"Rating: {info.Rating:F1}");

        return string.Join(" | ", parts);
    }
}
