using System.Diagnostics;

namespace MediaMatch.Infrastructure.Observability;

/// <summary>
/// Central OpenTelemetry configuration for MediaMatch.
/// Provides a shared <see cref="ActivitySource"/> for creating trace spans.
/// </summary>
public static class TelemetryConfig
{
    /// <summary>
    /// The shared ActivitySource for all MediaMatch instrumentation.
    /// </summary>
    public static readonly ActivitySource Source = new(ActivityNames.SourceName, "0.1.0");

    /// <summary>Starts a detection span.</summary>
    /// <param name="filePath">The path of the file being detected.</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> if tracing is disabled.</returns>
    public static Activity? StartDetect(string filePath) =>
        Source.StartActivity(ActivityNames.Detect)?
            .SetTag("mediamatch.file_name", Path.GetFileName(filePath));

    /// <summary>Starts a match span.</summary>
    /// <param name="mediaType">The media type being matched (e.g., "Movie", "Episode").</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> if tracing is disabled.</returns>
    public static Activity? StartMatch(string mediaType) =>
        Source.StartActivity(ActivityNames.Match)?
            .SetTag("mediamatch.media_type", mediaType);

    /// <summary>Starts a rename/file-organization span.</summary>
    /// <param name="fileCount">The number of files being organized.</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> if tracing is disabled.</returns>
    public static Activity? StartRename(int fileCount) =>
        Source.StartActivity(ActivityNames.Rename)?
            .SetTag("mediamatch.file_count", fileCount);

    /// <summary>Starts an API call span for a given provider.</summary>
    /// <param name="provider">The provider name (e.g., "TMDb", "TVDb").</param>
    /// <param name="operation">The operation being performed (e.g., "search", "get_details").</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> if tracing is disabled.</returns>
    public static Activity? StartApiCall(string provider, string operation) =>
        Source.StartActivity($"mediamatch.api.{provider.ToLowerInvariant()}")?
            .SetTag("mediamatch.provider", provider)
            .SetTag("mediamatch.operation", operation);

    /// <summary>Records an error on the current activity.</summary>
    /// <param name="activity">The activity to record the error on; does nothing when <see langword="null"/>.</param>
    /// <param name="ex">The exception to record.</param>
    public static void RecordError(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message }
        }));
    }
}
