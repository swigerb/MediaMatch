using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Scans directories for media files using parallel enumeration with
/// <see cref="Channel{T}"/> producer/consumer pattern for streaming results.
/// Reduces concurrency automatically for network paths.
/// </summary>
public sealed class ParallelFileScanner : IParallelFileScanner
{
    private static readonly ActivitySource Activity = new("MediaMatch", "0.1.0");

    private readonly PerformanceSettings _settings;
    private readonly INetworkPathDetector _networkDetector;
    private readonly ILogger<ParallelFileScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelFileScanner"/> class.
    /// </summary>
    /// <param name="settings">Performance settings controlling scan concurrency and depth.</param>
    /// <param name="networkDetector">Detector used to identify network paths for concurrency reduction.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ParallelFileScanner(
        PerformanceSettings settings,
        INetworkPathDetector networkDetector,
        ILogger<ParallelFileScanner>? logger = null)
    {
        _settings = settings;
        _networkDetector = networkDetector;
        _logger = logger ?? NullLogger<ParallelFileScanner>.Instance;
    }

    /// <inheritdoc />
    public ChannelReader<string> ScanAsync(
        string rootPath,
        IReadOnlySet<string>? allowedExtensions = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });

        _ = RunScanAsync(rootPath, allowedExtensions, progress, channel.Writer, ct);

        return channel.Reader;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ScanToListAsync(
        string rootPath,
        IReadOnlySet<string>? allowedExtensions = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        var reader = ScanAsync(rootPath, allowedExtensions, progress, ct);
        var results = new List<string>();

        try
        {
            await foreach (var file in reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                results.Add(file);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful cancellation — return whatever was collected so far
        }

        return results;
    }

    private async Task RunScanAsync(
        string rootPath,
        IReadOnlySet<string>? allowedExtensions,
        IProgress<ScanProgress>? progress,
        ChannelWriter<string> writer,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        int filesFound = 0;
        int filesProcessed = 0;

        bool isNetwork = _networkDetector.IsNetworkPath(rootPath);
        int concurrency = isNetwork ? _settings.NetworkConcurrency : _settings.MaxScanThreads;

        using var activity = Activity.StartActivity(
            isNetwork ? "mediamatch.scan.network" : "mediamatch.scan.parallel");
        activity?.SetTag("mediamatch.scan.root", rootPath);
        activity?.SetTag("mediamatch.scan.concurrency", concurrency);
        activity?.SetTag("mediamatch.scan.is_network", isNetwork);

        if (isNetwork)
        {
            _logger.LogInformation(
                "Network path detected — reducing concurrency to {Concurrency}", concurrency);
        }
        else
        {
            _logger.LogInformation(
                "Scanning {RootPath} with concurrency {Concurrency}", rootPath, concurrency);
        }

        try
        {
            // Collect all files lazily using streaming enumeration
            var files = EnumerateFilesRecursive(rootPath, allowedExtensions, _settings.MaxDirectoryDepth);

            await Parallel.ForEachAsync(
                files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = concurrency,
                    CancellationToken = ct
                },
                async (file, token) =>
                {
                    var found = Interlocked.Increment(ref filesFound);

                    progress?.Report(new ScanProgress(
                        found,
                        Volatile.Read(ref filesProcessed),
                        file,
                        sw.ElapsedMilliseconds));

                    await writer.WriteAsync(file, token).ConfigureAwait(false);
                    Interlocked.Increment(ref filesProcessed);
                }).ConfigureAwait(false);

            activity?.SetTag("mediamatch.scan.files_found", filesFound);
            _logger.LogInformation(
                "Scan complete: {FilesFound} files in {ElapsedMs}ms", filesFound, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scan cancelled after {FilesFound} files", filesFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed after {FilesFound} files", filesFound);
        }
        finally
        {
            progress?.Report(new ScanProgress(filesFound, filesProcessed, null, sw.ElapsedMilliseconds));
            writer.Complete();
        }
    }

    /// <summary>
    /// Lazily enumerates files using <see cref="Directory.EnumerateFiles"/>
    /// with configurable depth limit. Never loads full directory listing into memory.
    /// </summary>
    private IEnumerable<string> EnumerateFilesRecursive(
        string path,
        IReadOnlySet<string>? allowedExtensions,
        int maxDepth)
    {
        if (maxDepth < 0)
            yield break;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(path);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogDebug("Access denied: {Path}", path);
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            if (allowedExtensions is null || allowedExtensions.Contains(
                    Path.GetExtension(file).ToLowerInvariant()))
            {
                yield return file;
            }
        }

        IEnumerable<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(path);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (var dir in directories)
        {
            foreach (var file in EnumerateFilesRecursive(dir, allowedExtensions, maxDepth - 1))
            {
                yield return file;
            }
        }
    }
}
