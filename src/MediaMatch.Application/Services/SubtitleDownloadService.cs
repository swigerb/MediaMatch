using System.Text;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Application.Services;

/// <summary>
/// Downloads subtitle files and saves them alongside the video file.
/// Handles encoding detection and format-based file extension.
/// </summary>
public sealed class SubtitleDownloadService : ISubtitleDownloadService
{
    private readonly IEnumerable<ISubtitleProvider> _providers;
    private readonly ILogger<SubtitleDownloadService> _logger;

    public SubtitleDownloadService(
        IEnumerable<ISubtitleProvider> providers,
        ILogger<SubtitleDownloadService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> DownloadAndSaveAsync(
        SubtitleDescriptor subtitle,
        string videoFilePath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(videoFilePath);

        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, subtitle.ProviderName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No subtitle provider found for '{subtitle.ProviderName}'.");

        _logger.LogInformation(
            "Downloading subtitle '{SubName}' for '{Video}' from {Provider}",
            subtitle.Name, Path.GetFileName(videoFilePath), provider.Name);

        await using var stream = await provider.DownloadAsync(subtitle, ct);

        // Read content and detect encoding
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(ct);
        var detectedEncoding = reader.CurrentEncoding;

        // Build output path: same directory and base name as the video, with subtitle extension
        var directory = Path.GetDirectoryName(videoFilePath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(videoFilePath);
        var extension = GetExtension(subtitle.Format);
        var outputPath = Path.Combine(directory, $"{baseName}{extension}");

        // Write as UTF-8 with BOM for maximum player compatibility
        await File.WriteAllTextAsync(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);

        _logger.LogInformation(
            "Saved subtitle to {OutputPath} (detected encoding: {Encoding})",
            outputPath, detectedEncoding.EncodingName);

        return outputPath;
    }

    private static string GetExtension(SubtitleFormat format) => format switch
    {
        SubtitleFormat.SubRip => ".srt",
        SubtitleFormat.SubStationAlpha => ".ass",
        SubtitleFormat.MicroDVD => ".sub",
        SubtitleFormat.Sami => ".smi",
        SubtitleFormat.SubViewer => ".sub",
        _ => ".srt" // Default to .srt for unknown formats
    };
}
