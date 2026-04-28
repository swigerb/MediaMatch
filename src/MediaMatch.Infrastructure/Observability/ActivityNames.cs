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
    /// <summary>Activity name for media detection operations.</summary>
    public const string Detect = "mediamatch.detect";
    /// <summary>Activity name for media type detection.</summary>
    public const string DetectMediaType = "mediamatch.detect.media_type";
    /// <summary>Activity name for release information detection.</summary>
    public const string DetectReleaseInfo = "mediamatch.detect.release_info";

    // Matching
    /// <summary>Activity name for metadata matching operations.</summary>
    public const string Match = "mediamatch.match";
    /// <summary>Activity name for episode matching.</summary>
    public const string MatchEpisode = "mediamatch.match.episode";
    /// <summary>Activity name for movie matching.</summary>
    public const string MatchMovie = "mediamatch.match.movie";
    /// <summary>Activity name for batch matching operations.</summary>
    public const string MatchBatch = "mediamatch.match.batch";

    // API calls
    /// <summary>Activity name for TMDb API calls.</summary>
    public const string ApiTmdb = "mediamatch.api.tmdb";
    /// <summary>Activity name for TVDb API calls.</summary>
    public const string ApiTvdb = "mediamatch.api.tvdb";
    /// <summary>Activity name for provider search API calls.</summary>
    public const string ApiSearch = "mediamatch.api.search";
    /// <summary>Activity name for provider get-details API calls.</summary>
    public const string ApiGetDetails = "mediamatch.api.get_details";

    // File operations
    /// <summary>Activity name for file rename operations.</summary>
    public const string Rename = "mediamatch.rename";
    /// <summary>Activity name for rename preview (dry-run) operations.</summary>
    public const string RenamePreview = "mediamatch.rename.preview";
    /// <summary>Activity name for rename apply operations.</summary>
    public const string RenameApply = "mediamatch.rename.apply";
    /// <summary>Activity name for rename rollback operations.</summary>
    public const string RenameRollback = "mediamatch.rename.rollback";
    /// <summary>Activity name for file move operations.</summary>
    public const string FileOpsMove = "mediamatch.fileops.move";
    /// <summary>Activity name for file copy operations.</summary>
    public const string FileOpsCopy = "mediamatch.fileops.copy";

    // Cache
    /// <summary>Activity name for cache hit events.</summary>
    public const string CacheHit = "mediamatch.cache.hit";
    /// <summary>Activity name for cache miss events.</summary>
    public const string CacheMiss = "mediamatch.cache.miss";
}
