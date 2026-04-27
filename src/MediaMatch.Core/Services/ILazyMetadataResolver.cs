using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Wraps metadata providers with deferred execution. Metadata is only
/// fetched when explicitly requested (preview click or batch start).
/// </summary>
public interface ILazyMetadataResolver
{
    /// <summary>
    /// Registers a file path for later metadata resolution.
    /// Does not perform any API calls.
    /// </summary>
    void Register(string filePath, string cleanTitle, int? year = null);

    /// <summary>
    /// Resolves movie metadata for a previously registered file.
    /// Results are cached per session.
    /// </summary>
    Task<IReadOnlyList<Movie>> ResolveMovieAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Resolves episode search results for a previously registered file.
    /// Results are cached per session.
    /// </summary>
    Task<IReadOnlyList<SearchResult>> ResolveEpisodeSearchAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Clears all cached metadata for the current session.
    /// </summary>
    void ClearCache();
}
