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
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="cleanTitle">The cleaned title extracted from the filename.</param>
    /// <param name="year">An optional release year to narrow results.</param>
    void Register(string filePath, string cleanTitle, int? year = null);

    /// <summary>
    /// Resolves movie metadata for a previously registered file.
    /// Results are cached per session.
    /// </summary>
    /// <param name="filePath">The path to the registered media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching movies.</returns>
    Task<IReadOnlyList<Movie>> ResolveMovieAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Resolves episode search results for a previously registered file.
    /// Results are cached per session.
    /// </summary>
    /// <param name="filePath">The path to the registered media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching search results.</returns>
    Task<IReadOnlyList<SearchResult>> ResolveEpisodeSearchAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Clears all cached metadata for the current session.
    /// </summary>
    void ClearCache();
}
