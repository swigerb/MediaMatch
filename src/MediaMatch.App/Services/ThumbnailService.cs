using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MediaMatch.App.Services;

/// <summary>
/// Generates and caches video thumbnails using ffmpeg.
/// Returns null if ffmpeg is not available — UI shows a placeholder icon.
/// </summary>
public sealed class ThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;
    private readonly string _cacheDir;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public ThumbnailService(ILogger<ThumbnailService>? logger = null)
    {
        _logger = logger ?? NullLogger<ThumbnailService>.Instance;
        _cacheDir = Path.Combine(Path.GetTempPath(), "MediaMatch", "thumbs");
    }

    /// <summary>
    /// Gets or generates a thumbnail BitmapImage for the given video file.
    /// Returns null if ffmpeg is not installed or generation fails.
    /// </summary>
    public async Task<BitmapImage?> GetThumbnailAsync(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            return null;

        try
        {
            Directory.CreateDirectory(_cacheDir);

            // Use a hash of the full path for cache key
            var hash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(videoPath)))[..16];
            var thumbPath = Path.Combine(_cacheDir, $"{hash}.jpg");

            // Return cached thumbnail if available
            if (File.Exists(thumbPath))
            {
                return await LoadBitmapAsync(thumbPath);
            }

            // Generate with ffmpeg
            if (!await IsFfmpegAvailableAsync())
            {
                _logger.LogDebug("ffmpeg not found on PATH — thumbnail generation disabled");
                return null;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (File.Exists(thumbPath))
                    return await LoadBitmapAsync(thumbPath);

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    ArgumentList =
                    {
                        "-ss", "00:00:30",
                        "-i", videoPath,
                        "-frames:v", "1",
                        "-q:v", "5",
                        "-vf", "scale=160:-1",
                        "-y",
                        thumbPath
                    },
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process is null) return null;

                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || !File.Exists(thumbPath))
                {
                    _logger.LogWarning("ffmpeg thumbnail generation failed for {Path} (exit code {Code})",
                        videoPath, process.ExitCode);
                    return null;
                }

                return await LoadBitmapAsync(thumbPath);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail for {Path}", videoPath);
            return null;
        }
    }

    private static async Task<BitmapImage?> LoadBitmapAsync(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            using var stream = File.OpenRead(path);
            var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await stream.CopyToAsync(memStream.AsStreamForWrite());
            memStream.Seek(0);
            await bitmap.SetSourceAsync(memStream);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> IsFfmpegAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
