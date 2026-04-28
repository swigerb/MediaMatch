using MediaMatch.Application.Expressions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;

namespace MediaMatch.Application.Services;

/// <summary>
/// Generates a preview of rename operations without modifying the file system.
/// Uses the Scriban expression engine and matching pipeline.
/// </summary>
public sealed class RenamePreviewService : IRenamePreviewService
{
    private readonly IMatchingPipeline _pipeline;
    private readonly IExpressionEngine _expressionEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenamePreviewService"/> class.
    /// </summary>
    /// <param name="pipeline">The matching pipeline used to detect and match media files.</param>
    /// <param name="expressionEngine">The expression engine used to evaluate rename patterns.</param>
    public RenamePreviewService(IMatchingPipeline pipeline, IExpressionEngine expressionEngine)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(expressionEngine);

        _pipeline = pipeline;
        _expressionEngine = expressionEngine;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileOrganizationResult>> PreviewAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(renamePattern);

        try
        {
            if (!_expressionEngine.Validate(renamePattern, out var validationError))
            {
                return filePaths.Select(fp =>
                    FileOrganizationResult.Failed(fp, $"Invalid pattern: {validationError}")).ToList();
            }
        }
        catch (Exception ex)
        {
            return filePaths.Select(fp =>
                FileOrganizationResult.Failed(fp, $"Invalid pattern: {ex.Message}")).ToList();
        }

        var results = new List<FileOrganizationResult>(filePaths.Count);

        for (int i = 0; i < filePaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var filePath = filePaths[i];

            try
            {
                var matchResult = await _pipeline.ProcessAsync(filePath, ct).ConfigureAwait(false);
                var result = GeneratePreview(filePath, matchResult, renamePattern);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(FileOrganizationResult.Failed(filePath, ex.Message));
            }
        }

        return results;
    }

    private FileOrganizationResult GeneratePreview(
        string filePath,
        MatchResult matchResult,
        string renamePattern)
    {
        if (!matchResult.IsMatch)
        {
            return new FileOrganizationResult(
                filePath, null, 0f, matchResult.MediaType,
                ["No match found from any provider"], Success: false);
        }

        var bindings = CreateBindings(matchResult, filePath);
        var newName = _expressionEngine.Evaluate(renamePattern, bindings);

        if (string.IsNullOrWhiteSpace(newName))
        {
            return new FileOrganizationResult(
                filePath, null, matchResult.Confidence, matchResult.MediaType,
                ["Pattern produced empty result"], Success: false);
        }

        // Preserve original extension
        var ext = Path.GetExtension(filePath);
        if (!string.IsNullOrEmpty(ext) && !newName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            newName += ext;

        // Build full new path relative to the original file's directory
        var dir = Path.GetDirectoryName(filePath);
        var newPath = string.IsNullOrEmpty(dir) ? newName : Path.Combine(dir, newName);

        var warnings = new List<string>();
        if (matchResult.Confidence < 0.5f)
            warnings.Add("Low confidence match");

        return new FileOrganizationResult(
            filePath, newPath, matchResult.Confidence, matchResult.MediaType,
            warnings, Success: true);
    }

    private static IMediaBindings CreateBindings(MatchResult matchResult, string filePath)
    {
        if (matchResult.Episode is not null)
            return MediaBindings.ForEpisode(matchResult.Episode, matchResult.SeriesInfo, filePath);

        if (matchResult.Movie is not null)
            return MediaBindings.ForMovie(matchResult.Movie, matchResult.MovieInfo, filePath);

        // Fallback: minimal bindings
        return MediaBindings.ForMovie(
            new Movie("Unknown", 0), filePath: filePath);
    }
}
