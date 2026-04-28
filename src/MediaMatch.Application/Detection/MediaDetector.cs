using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

/// <summary>
/// Detects the media type of a file based on its extension, filename patterns, and release info.
/// </summary>
public sealed class MediaDetector
{
    private static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".flv", ".mov", ".m4v",
            ".mpg", ".mpeg", ".ts", ".m2ts", ".vob", ".webm", ".ogv",
        };

    private static readonly HashSet<string> SubtitleExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".srt", ".sub", ".ssa", ".ass", ".idx", ".vtt",
        };

    private static readonly HashSet<string> AudioExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".ogg", ".m4a", ".wav", ".wma", ".aac", ".opus",
        };

    private static readonly string[] AnimeIndicators =
        ["[SubGroup]", "FLAC", "Hi10P", "BD", "BDRip"];

    private readonly ReleaseInfoParser _releaseParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaDetector"/> class.
    /// </summary>
    public MediaDetector()
    {
        _releaseParser = new ReleaseInfoParser();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaDetector"/> class.
    /// </summary>
    /// <param name="releaseParser">The release info parser to use for filename analysis.</param>
    public MediaDetector(ReleaseInfoParser releaseParser)
    {
        _releaseParser = releaseParser;
    }

    /// <summary>Detect whether a file is a movie, TV episode, anime, music, etc.</summary>
    public MediaType DetectMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath);

        if (SubtitleExtensions.Contains(ext))
            return MediaType.Subtitle;

        if (AudioExtensions.Contains(ext))
            return MediaType.Music;

        if (!VideoExtensions.Contains(ext) && !string.IsNullOrEmpty(ext))
            return MediaType.Unknown;

        var fileName = Path.GetFileName(filePath);
        var se = _releaseParser.ParseSeasonEpisode(fileName);

        if (LooksLikeAnime(fileName, se))
            return MediaType.Anime;

        if (se is not null)
            return MediaType.TvSeries;

        return MediaType.Movie;
    }

    /// <summary>Extract all available info from a file path.</summary>
    public DetectionResult Detect(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var releaseInfo = _releaseParser.Parse(fileName);
        var mediaType = DetectMediaType(filePath);
        float confidence = ComputeConfidence(mediaType, releaseInfo);

        return new DetectionResult(filePath, mediaType, releaseInfo, confidence);
    }

    /// <summary>Detect media type for a batch of files.</summary>
    public IReadOnlyList<DetectionResult> DetectBatch(IReadOnlyList<string> filePaths)
    {
        var results = new DetectionResult[filePaths.Count];
        for (int i = 0; i < filePaths.Count; i++)
        {
            results[i] = Detect(filePaths[i]);
        }

        return results;
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private static bool LooksLikeAnime(string fileName, SeasonEpisodeMatch? se)
    {
        // Bracketed group at start is a strong anime signal: [SubGroup] Title - 01.mkv
        if (fileName.StartsWith('['))
        {
            // Anime typically uses absolute episode numbering or has bracketed tags
            if (se is { AbsoluteNumber: not null })
                return true;

            // Check for common anime release patterns
            if (fileName.Contains("1080p", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("720p", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("HEVC", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("FLAC", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("Hi10P", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Absolute episode with no season marker is often anime
        if (se is { AbsoluteNumber: not null, Season: 1 }
            && !fileName.Contains("S01", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static float ComputeConfidence(MediaType mediaType, ReleaseInfo info)
    {
        if (mediaType == MediaType.Unknown)
            return 0.1f;

        float confidence = 0.4f;

        if (!string.IsNullOrWhiteSpace(info.CleanTitle))
            confidence += 0.1f;

        if (info.Year.HasValue)
            confidence += 0.1f;

        if (info.Quality != VideoQuality.Unknown)
            confidence += 0.1f;

        if (info.VideoSource is not null)
            confidence += 0.1f;

        if (info.SeasonEpisode is not null)
            confidence += 0.1f;

        if (info.VideoCodec is not null || info.AudioCodec is not null)
            confidence += 0.05f;

        if (info.ReleaseGroup is not null)
            confidence += 0.05f;

        return Math.Min(confidence, 1.0f);
    }
}
