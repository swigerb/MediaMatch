using System.Diagnostics;
using MediaMatch.Application.Detection;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Matching;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Pipeline;

/// <summary>
/// Coordinates: Detect media type → Match against providers → Apply rename template.
/// Configurable pipeline with short-circuit on high-confidence match.
/// Falls back to opportunistic matching when strict matching fails.
/// </summary>
public sealed class MatchingPipeline : IMatchingPipeline
{
    private static readonly ActivitySource Activity = new("MediaMatch", "0.1.0");

    private readonly MediaDetector _detector;
    private readonly ReleaseInfoParser _releaseParser;
    private readonly IReadOnlyList<IEpisodeProvider> _episodeProviders;
    private readonly IReadOnlyList<IMovieProvider> _movieProviders;
    private readonly EpisodeMatcher _episodeMatcher;
    private readonly OpportunisticMatcher _opportunisticMatcher;
    private readonly bool _enableOpportunisticMode;
    private readonly ILogger<MatchingPipeline> _logger;

    /// <summary>Confidence threshold above which we short-circuit and stop searching.</summary>
    private const float HighConfidenceThreshold = 0.85f;

    /// <summary>Most recent opportunistic suggestions from the last ProcessAsync call.</summary>
    public IReadOnlyList<MatchSuggestion> LastSuggestions { get; private set; } = Array.Empty<MatchSuggestion>();

    public MatchingPipeline(
        IEnumerable<IEpisodeProvider> episodeProviders,
        IEnumerable<IMovieProvider> movieProviders,
        ILogger<MatchingPipeline>? logger = null,
        AppSettings? appSettings = null)
    {
        ArgumentNullException.ThrowIfNull(episodeProviders);
        ArgumentNullException.ThrowIfNull(movieProviders);

        _releaseParser = new ReleaseInfoParser();
        _detector = new MediaDetector(_releaseParser);
        _episodeProviders = episodeProviders.ToList();
        _movieProviders = movieProviders.ToList();
        _episodeMatcher = new EpisodeMatcher();
        _logger = logger ?? NullLogger<MatchingPipeline>.Instance;
        _enableOpportunisticMode = appSettings?.EnableOpportunisticMode ?? true;
        _opportunisticMatcher = new OpportunisticMatcher(_episodeProviders, _movieProviders, logger: null);
    }

    public MatchingPipeline(
        MediaDetector detector,
        ReleaseInfoParser releaseParser,
        EpisodeMatcher episodeMatcher,
        IEnumerable<IEpisodeProvider> episodeProviders,
        IEnumerable<IMovieProvider> movieProviders,
        ILogger<MatchingPipeline>? logger = null)
    {
        _detector = detector;
        _releaseParser = releaseParser;
        _episodeMatcher = episodeMatcher;
        _episodeProviders = episodeProviders.ToList();
        _movieProviders = movieProviders.ToList();
        _logger = logger ?? NullLogger<MatchingPipeline>.Instance;
        _enableOpportunisticMode = true;
        _opportunisticMatcher = new OpportunisticMatcher(_episodeProviders, _movieProviders, logger: null);
    }

    public async Task<MatchResult> ProcessAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var activity = Activity.StartActivity("mediamatch.match");
        activity?.SetTag("mediamatch.file_name", Path.GetFileName(filePath));

        // Stage 1: Detect media type
        var detection = _detector.Detect(filePath);
        activity?.SetTag("mediamatch.media_type", detection.MediaType.ToString());
        _logger.LogDebug("Detected {MediaType} for {FileName} (confidence={Confidence:F2})",
            detection.MediaType, Path.GetFileName(filePath), detection.Confidence);

        // Stage 2: Match against providers based on detected type
        var result = detection.MediaType switch
        {
            MediaType.TvSeries or MediaType.Anime =>
                await MatchEpisodeAsync(filePath, detection, ct),
            MediaType.Movie =>
                await MatchMovieAsync(filePath, detection, ct),
            _ =>
                MatchResult.NoMatch(detection.MediaType)
        };

        activity?.SetTag("mediamatch.confidence", result.Confidence);
        activity?.SetTag("mediamatch.provider", result.ProviderSource);

        // Opportunistic fallback when strict matching fails
        if (result.Confidence < HighConfidenceThreshold && _enableOpportunisticMode)
        {
            _logger.LogInformation("Strict matching below threshold ({Confidence:F2}), trying opportunistic mode",
                result.Confidence);
            LastSuggestions = await _opportunisticMatcher.SuggestAsync(filePath, detection, ct);
            activity?.SetTag("mediamatch.opportunistic_suggestions", LastSuggestions.Count);
        }
        else
        {
            LastSuggestions = Array.Empty<MatchSuggestion>();
        }

        return result;
    }

    public async Task<IReadOnlyList<MatchResult>> ProcessBatchAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var results = new MatchResult[filePaths.Count];
        for (int i = 0; i < filePaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            results[i] = await ProcessAsync(filePaths[i], ct);
        }

        return results;
    }

    private async Task<MatchResult> MatchEpisodeAsync(
        string filePath, DetectionResult detection, CancellationToken ct)
    {
        var releaseInfo = detection.ReleaseInfo;
        var searchQuery = releaseInfo.CleanTitle;

        if (string.IsNullOrWhiteSpace(searchQuery))
            return MatchResult.NoMatch(detection.MediaType);

        MatchResult? bestResult = null;

        foreach (var provider in _episodeProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var searchResults = await provider.SearchAsync(searchQuery, ct);
                if (searchResults.Count == 0)
                    continue;

                var series = searchResults[0];
                var episodes = await provider.GetEpisodesAsync(series, ct: ct);

                // Use the existing EpisodeMatcher to find the best match
                var matches = _episodeMatcher.MatchFiles([filePath], episodes);

                if (matches.Count > 0)
                {
                    var match = matches[0];
                    var seriesInfo = await provider.GetSeriesInfoAsync(series, ct);
                    var confidence = match.Score * detection.Confidence;

                    var result = new MatchResult(
                        detection.MediaType,
                        confidence,
                        provider.Name,
                        Episode: match.Candidate,
                        SeriesInfo: seriesInfo);

                    if (bestResult is null || confidence > bestResult.Confidence)
                        bestResult = result;

                    // Short-circuit on high confidence
                    if (confidence >= HighConfidenceThreshold)
                        return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Episode provider {Provider} failed for query '{Query}'",
                    provider.Name, searchQuery);
                // Provider failure — continue to next provider
            }
        }

        return bestResult ?? MatchResult.NoMatch(detection.MediaType);
    }

    private async Task<MatchResult> MatchMovieAsync(
        string filePath, DetectionResult detection, CancellationToken ct)
    {
        var releaseInfo = detection.ReleaseInfo;
        var searchQuery = releaseInfo.CleanTitle;

        if (string.IsNullOrWhiteSpace(searchQuery))
            return MatchResult.NoMatch(MediaType.Movie);

        MatchResult? bestResult = null;

        foreach (var provider in _movieProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var movies = await provider.SearchAsync(searchQuery, releaseInfo.Year, ct);
                if (movies.Count == 0)
                    continue;

                var movie = movies[0];
                var movieInfo = await provider.GetMovieInfoAsync(movie, ct);
                var confidence = ComputeMovieConfidence(detection, movie);

                var result = new MatchResult(
                    MediaType.Movie,
                    confidence,
                    provider.Name,
                    Movie: movie,
                    MovieInfo: movieInfo);

                if (bestResult is null || confidence > bestResult.Confidence)
                    bestResult = result;

                if (confidence >= HighConfidenceThreshold)
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Movie provider {Provider} failed for query '{Query}'",
                    provider.Name, searchQuery);
                // Provider failure — continue to next
            }
        }

        return bestResult ?? MatchResult.NoMatch(MediaType.Movie);
    }

    private static float ComputeMovieConfidence(DetectionResult detection, Movie movie)
    {
        float confidence = detection.Confidence;
        var releaseInfo = detection.ReleaseInfo;

        // Boost confidence when year matches
        if (releaseInfo.Year.HasValue && releaseInfo.Year.Value == movie.Year)
            confidence = Math.Min(confidence + 0.15f, 1.0f);

        // Boost on title similarity
        var cleanDetected = Matching.Normalization.NormalizeName(releaseInfo.CleanTitle);
        var cleanMovie = Matching.Normalization.NormalizeName(movie.Name);
        if (string.Equals(cleanDetected, cleanMovie, StringComparison.OrdinalIgnoreCase))
            confidence = Math.Min(confidence + 0.2f, 1.0f);

        return confidence;
    }
}
