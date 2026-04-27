namespace MediaMatch.Infrastructure.Observability;

/// <summary>
/// Constants for OpenTelemetry activity/span names used across MediaMatch.
/// Organized by domain to keep instrumentation consistent.
/// </summary>
public static class ActivityNames
{
    /// <summary>Source name for all MediaMatch activities.</summary>
    public const string SourceName = "MediaMatch";

    // Detection
    public const string Detect = "mediamatch.detect";
    public const string DetectMediaType = "mediamatch.detect.media_type";
    public const string DetectReleaseInfo = "mediamatch.detect.release_info";

    // Matching
    public const string Match = "mediamatch.match";
    public const string MatchEpisode = "mediamatch.match.episode";
    public const string MatchMovie = "mediamatch.match.movie";
    public const string MatchBatch = "mediamatch.match.batch";

    // API calls
    public const string ApiTmdb = "mediamatch.api.tmdb";
    public const string ApiTvdb = "mediamatch.api.tvdb";
    public const string ApiSearch = "mediamatch.api.search";
    public const string ApiGetDetails = "mediamatch.api.get_details";

    // File operations
    public const string Rename = "mediamatch.rename";
    public const string RenamePreview = "mediamatch.rename.preview";
    public const string RenameApply = "mediamatch.rename.apply";
    public const string RenameRollback = "mediamatch.rename.rollback";
    public const string FileOpsMove = "mediamatch.fileops.move";
    public const string FileOpsCopy = "mediamatch.fileops.copy";

    // Cache
    public const string CacheHit = "mediamatch.cache.hit";
    public const string CacheMiss = "mediamatch.cache.miss";
}
