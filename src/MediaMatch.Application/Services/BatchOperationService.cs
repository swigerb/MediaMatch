using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Processes multiple file renames with configurable concurrency, progress reporting,
/// and cancellation support.
/// </summary>
public sealed class BatchOperationService : IBatchOperationService
{
    private readonly IFileOrganizationService _organizationService;
    private readonly ILogger<BatchOperationService> _logger;
    private readonly int _maxConcurrency;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchOperationService"/> class.
    /// </summary>
    /// <param name="organizationService">The file organization service used to process individual renames.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent rename operations. Defaults to 4.</param>
    public BatchOperationService(
        IFileOrganizationService organizationService,
        ILogger<BatchOperationService>? logger = null,
        int maxConcurrency = 4)
    {
        ArgumentNullException.ThrowIfNull(organizationService);
        _organizationService = organizationService;
        _logger = logger ?? NullLogger<BatchOperationService>.Instance;
        _maxConcurrency = Math.Max(1, maxConcurrency);
    }

    /// <inheritdoc/>
    public async Task<BatchJob> ExecuteAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var batchItems = filePaths
            .Select(fp => new BatchFileItem { FilePath = fp })
            .ToList();

        var job = new BatchJob
        {
            Files = batchItems,
            Status = BatchStatus.Running,
            StartedAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation("Starting batch operation for {FileCount} files (concurrency={Concurrency})",
            filePaths.Count, _maxConcurrency);

        int completed = 0;
        int failed = 0;

        try
        {
            // Process files in chunks to control concurrency
            var chunks = filePaths
                .Select((path, index) => (path, index))
                .Chunk(_maxConcurrency);

            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();

                var tasks = chunk.Select(async item =>
                {
                    var (path, index) = item;
                    var batchItem = batchItems[index];
                    batchItem.Status = BatchFileStatus.Processing;

                    job.CurrentFile = Path.GetFileName(path);
                    progress?.Report(new BatchProgress(filePaths.Count, completed, failed, job.CurrentFile));

                    try
                    {
                        var results = await _organizationService.OrganizeAsync(
                            [path], renamePattern, ct).ConfigureAwait(false);

                        var result = results.FirstOrDefault();
                        if (result is not null && result.Success)
                        {
                            batchItem.NewPath = result.NewPath;
                            batchItem.Status = BatchFileStatus.Success;
                            Interlocked.Increment(ref completed);
                        }
                        else
                        {
                            batchItem.Status = BatchFileStatus.Failed;
                            batchItem.Error = result?.Warnings.FirstOrDefault() ?? "Unknown error";
                            Interlocked.Increment(ref failed);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        batchItem.Status = BatchFileStatus.Failed;
                        batchItem.Error = ex.Message;
                        Interlocked.Increment(ref failed);
                        _logger.LogWarning(ex, "Failed to process {File}", path);
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);

                job.CompletedCount = completed;
                job.FailedCount = failed;
                progress?.Report(new BatchProgress(filePaths.Count, completed, failed, job.CurrentFile));
            }

            job.Status = failed == filePaths.Count ? BatchStatus.Failed : BatchStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            job.Status = BatchStatus.Cancelled;
            // Mark remaining pending items as skipped
            foreach (var item in batchItems.Where(i => i.Status == BatchFileStatus.Pending))
            {
                item.Status = BatchFileStatus.Skipped;
            }
            _logger.LogInformation("Batch operation cancelled after {Completed}/{Total} files",
                completed, filePaths.Count);
        }

        job.CompletedCount = completed;
        job.FailedCount = failed;
        job.CompletedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Batch operation finished: {Status}, {Completed} succeeded, {Failed} failed",
            job.Status, completed, failed);

        return job;
    }
}
