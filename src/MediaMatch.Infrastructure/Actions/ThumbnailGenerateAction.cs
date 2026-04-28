using System.Diagnostics;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Actions;

/// <summary>
/// Post-process action that extracts a thumbnail from a video file using ffmpeg.
/// Saves a .jpg alongside the video file.
/// </summary>
public sealed class ThumbnailGenerateAction : IPostProcessAction
{
    private readonly ILogger<ThumbnailGenerateAction> _logger;
    private readonly string? _ffmpegPath;

    /// <inheritdoc />
    public string Name => "thumbnail";

    /// <inheritdoc />
    public bool IsAvailable => _ffmpegPath is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailGenerateAction"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ThumbnailGenerateAction(ILogger<ThumbnailGenerateAction>? logger = null)
    {
        _logger = logger ?? NullLogger<ThumbnailGenerateAction>.Instance;
        _ffmpegPath = FindFfmpeg();
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default)
    {
        if (!IsAvailable || _ffmpegPath is null)
        {
            _logger.LogWarning("Thumbnail generation skipped — ffmpeg not found");
            return;
        }

        var videoPath = result.NewPath ?? result.OriginalPath;
        if (!File.Exists(videoPath))
        {
            _logger.LogWarning("Thumbnail skipped — file not found: {Path}", videoPath);
            return;
        }

        var outputPath = Path.ChangeExtension(videoPath, ".jpg");

        var args = $"-y -i \"{videoPath}\" -vf \"thumbnail=300\" -frames:v 1 \"{outputPath}\"";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode == 0)
                _logger.LogInformation("Thumbnail generated: {Path}", outputPath);
            else
                _logger.LogWarning("ffmpeg exited with code {Code} for {Path}", process.ExitCode, videoPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail generation failed for {Path}", videoPath);
        }
    }

    private static string? FindFfmpeg()
    {
        // Check common locations
        var candidates = new[]
        {
            "ffmpeg",
            "ffmpeg.exe",
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\ProgramData\chocolatey\bin\ffmpeg.exe"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
                if (p?.ExitCode == 0) return candidate;
            }
            catch
            {
                // Not found at this path
            }
        }

        return null;
    }
}
