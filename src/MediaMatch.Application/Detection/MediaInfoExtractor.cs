using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using MediaMatch.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Detection;

/// <summary>
/// Extracts technical metadata from video files by shelling out to ffprobe
/// (when available) or falling back to filename-based detection.
/// </summary>
public sealed partial class MediaInfoExtractor
{
    private readonly ILogger<MediaInfoExtractor> _logger;

    public MediaInfoExtractor(ILogger<MediaInfoExtractor>? logger = null)
    {
        _logger = logger ?? NullLogger<MediaInfoExtractor>.Instance;
    }

    /// <summary>
    /// Extracts technical metadata from a video file.
    /// Tries ffprobe first, falls back to filename parsing.
    /// </summary>
    public async Task<MediaTechnicalInfo> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        // Try ffprobe first
        try
        {
            var ffprobeResult = await RunFfprobeAsync(filePath, ct);
            if (ffprobeResult is not null)
                return ffprobeResult;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ffprobe not available or failed, falling back to filename parsing");
        }

        // Fallback: parse from filename
        return ParseFromFileName(filePath);
    }

    /// <summary>
    /// Parses technical metadata from a filename (no file access required).
    /// </summary>
    public MediaTechnicalInfo ParseFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        return new MediaTechnicalInfo(
            AudioChannels: DetectAudioChannels(fileName),
            DolbyVision: DetectDolbyVision(fileName),
            HdrFormat: DetectHdrFormat(fileName),
            Resolution: DetectResolution(fileName),
            BitDepth: DetectBitDepth(fileName),
            VideoCodec: DetectVideoCodec(fileName),
            AudioCodec: DetectAudioCodec(fileName));
    }

    // ── ffprobe integration ──────────────────────────────────────

    private async Task<MediaTechnicalInfo?> RunFfprobeAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
            return null;

        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -print_format json -show_streams -show_format \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // ffprobe not found on PATH
            return null;
        }

        var output = await process.StandardOutput.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        return ParseFfprobeOutput(output, filePath);
    }

    private MediaTechnicalInfo? ParseFfprobeOutput(string json, string filePath)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("streams", out var streams))
                return null;

            string videoCodec = "Unknown";
            string resolution = "SD";
            string bitDepth = "8bit";
            string? hdrFormat = null;
            string? dolbyVision = null;
            string audioCodec = "Unknown";
            string audioChannels = "2.0 Stereo";

            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.GetProperty("codec_type").GetString();

                if (codecType == "video")
                {
                    // Video codec
                    var codecName = stream.GetProperty("codec_name").GetString() ?? "";
                    videoCodec = MapVideoCodec(codecName);

                    // Resolution
                    if (stream.TryGetProperty("height", out var heightProp))
                    {
                        var height = heightProp.GetInt32();
                        resolution = MapResolution(height);
                    }

                    // Bit depth
                    if (stream.TryGetProperty("bits_per_raw_sample", out var bitsProp))
                    {
                        var bits = bitsProp.GetString();
                        bitDepth = bits == "10" ? "10bit" : "8bit";
                    }
                    else if (stream.TryGetProperty("pix_fmt", out var pixFmt))
                    {
                        var fmt = pixFmt.GetString() ?? "";
                        bitDepth = fmt.Contains("10") ? "10bit" : "8bit";
                    }

                    // HDR / Dolby Vision detection from side data
                    if (stream.TryGetProperty("side_data_list", out var sideData))
                    {
                        foreach (var sd in sideData.EnumerateArray())
                        {
                            var sdType = sd.TryGetProperty("side_data_type", out var sdtProp)
                                ? sdtProp.GetString() : null;

                            if (sdType?.Contains("Dolby Vision") == true)
                            {
                                dolbyVision = "DV";
                                if (sd.TryGetProperty("dv_profile", out var dvProfile))
                                    dolbyVision = $"DoVi P{dvProfile.GetInt32()}";
                            }

                            if (sdType?.Contains("Mastering display") == true ||
                                sdType?.Contains("Content light level") == true)
                            {
                                hdrFormat ??= "HDR10";
                            }
                        }
                    }

                    // Color transfer for HDR detection
                    if (stream.TryGetProperty("color_transfer", out var colorTransfer))
                    {
                        var ct = colorTransfer.GetString();
                        if (ct == "smpte2084")
                            hdrFormat ??= "HDR10";
                        else if (ct == "arib-std-b67")
                            hdrFormat = "HLG";
                    }
                }
                else if (codecType == "audio" && audioCodec == "Unknown")
                {
                    var codecName = stream.GetProperty("codec_name").GetString() ?? "";
                    audioCodec = MapAudioCodec(codecName);

                    if (stream.TryGetProperty("channels", out var channelsProp))
                    {
                        var channels = channelsProp.GetInt32();
                        audioChannels = MapAudioChannels(channels, audioCodec);
                    }
                    else if (stream.TryGetProperty("channel_layout", out var layoutProp))
                    {
                        audioChannels = MapChannelLayout(layoutProp.GetString() ?? "stereo");
                    }
                }
            }

            return new MediaTechnicalInfo(
                AudioChannels: audioChannels,
                DolbyVision: dolbyVision,
                HdrFormat: hdrFormat,
                Resolution: resolution,
                BitDepth: bitDepth,
                VideoCodec: videoCodec,
                AudioCodec: audioCodec);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse ffprobe output for {File}", filePath);
            return null;
        }
    }

    // ── Filename-based detection ─────────────────────────────────

    [GeneratedRegex(@"\b(?:7\.1\s*Atmos|7\.1)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Channels71Regex();

    [GeneratedRegex(@"\b5\.1\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Channels51Regex();

    [GeneratedRegex(@"\bAtmos\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AtmosRegex();

    [GeneratedRegex(@"\b(?:DoVi|DV|Dolby\.?Vision)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DolbyVisionRegex();

    [GeneratedRegex(@"\bDoVi\s*P(\d)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DoViProfileRegex();

    [GeneratedRegex(@"\bHDR10\+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Hdr10PlusRegex();

    [GeneratedRegex(@"\bHDR10\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Hdr10Regex();

    [GeneratedRegex(@"\bHLG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HlgRegex();

    [GeneratedRegex(@"\bHDR\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HdrGenericRegex();

    [GeneratedRegex(@"\b(?:2160[pi]|4[Kk]|UHD)\b", RegexOptions.Compiled)]
    private static partial Regex Resolution4KRegex();

    [GeneratedRegex(@"\b(?:1440[pi]|QHD|2[Kk])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ResolutionQhdRegex();

    [GeneratedRegex(@"\b1080[pi]\b", RegexOptions.Compiled)]
    private static partial Regex Resolution1080Regex();

    [GeneratedRegex(@"\b720[pi]\b", RegexOptions.Compiled)]
    private static partial Regex Resolution720Regex();

    [GeneratedRegex(@"\b10bit\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BitDepth10Regex();

    [GeneratedRegex(@"\b(?:HEVC|[xXhH]\.?265)\b", RegexOptions.Compiled)]
    private static partial Regex VideoHevcRegex();

    [GeneratedRegex(@"\bAV1\b", RegexOptions.Compiled)]
    private static partial Regex VideoAv1Regex();

    [GeneratedRegex(@"\b(?:AVC|[xXhH]\.?264)\b", RegexOptions.Compiled)]
    private static partial Regex VideoH264Regex();

    [GeneratedRegex(@"\bVP9\b", RegexOptions.Compiled)]
    private static partial Regex VideoVp9Regex();

    [GeneratedRegex(@"\b(?:TrueHD\s*Atmos|TrueHD)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioTrueHdRegex();

    [GeneratedRegex(@"\b(?:DTS-HD\s*MA|DTS-HD\.?MA|DTS-X|DTS-HD|DTS)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioDtsRegex();

    [GeneratedRegex(@"\b(?:EAC-?3|DD\+|DDP?5?\.?1?|AC-?3|Dolby\s*Digital)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioAc3Regex();

    [GeneratedRegex(@"\bAAC\b", RegexOptions.Compiled)]
    private static partial Regex AudioAacRegex();

    [GeneratedRegex(@"\bFLAC\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioFlacRegex();

    [GeneratedRegex(@"\bOPUS\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioOpusRegex();

    private static string DetectAudioChannels(string fileName)
    {
        if (Channels71Regex().IsMatch(fileName))
            return AtmosRegex().IsMatch(fileName) ? "7.1 Atmos" : "7.1";
        if (Channels51Regex().IsMatch(fileName))
            return "5.1";
        if (AtmosRegex().IsMatch(fileName))
            return "7.1 Atmos";
        return "2.0 Stereo";
    }

    private static string? DetectDolbyVision(string fileName)
    {
        var profileMatch = DoViProfileRegex().Match(fileName);
        if (profileMatch.Success)
            return $"DoVi P{profileMatch.Groups[1].Value}";
        if (DolbyVisionRegex().IsMatch(fileName))
            return "DV";
        return null;
    }

    private static string? DetectHdrFormat(string fileName)
    {
        if (Hdr10PlusRegex().IsMatch(fileName)) return "HDR10+";
        if (Hdr10Regex().IsMatch(fileName)) return "HDR10";
        if (HlgRegex().IsMatch(fileName)) return "HLG";
        if (HdrGenericRegex().IsMatch(fileName)) return "HDR10";
        return null;
    }

    private static string DetectResolution(string fileName)
    {
        if (Resolution4KRegex().IsMatch(fileName)) return "UHD";
        if (ResolutionQhdRegex().IsMatch(fileName)) return "QHD";
        if (Resolution1080Regex().IsMatch(fileName)) return "1080p";
        if (Resolution720Regex().IsMatch(fileName)) return "720p";
        return "SD";
    }

    private static string DetectBitDepth(string fileName)
    {
        return BitDepth10Regex().IsMatch(fileName) ? "10bit" : "8bit";
    }

    private static string DetectVideoCodec(string fileName)
    {
        if (VideoHevcRegex().IsMatch(fileName)) return "HEVC";
        if (VideoAv1Regex().IsMatch(fileName)) return "AV1";
        if (VideoH264Regex().IsMatch(fileName)) return "H.264";
        if (VideoVp9Regex().IsMatch(fileName)) return "VP9";
        return "Unknown";
    }

    private static string DetectAudioCodec(string fileName)
    {
        if (AudioTrueHdRegex().IsMatch(fileName))
            return AtmosRegex().IsMatch(fileName) ? "TrueHD Atmos" : "TrueHD";
        if (AudioDtsRegex().IsMatch(fileName))
        {
            if (fileName.Contains("DTS-HD MA", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("DTS-HD.MA", StringComparison.OrdinalIgnoreCase))
                return "DTS-HD MA";
            if (fileName.Contains("DTS-X", StringComparison.OrdinalIgnoreCase))
                return "DTS-X";
            if (fileName.Contains("DTS-HD", StringComparison.OrdinalIgnoreCase))
                return "DTS-HD";
            return "DTS";
        }
        if (AudioAc3Regex().IsMatch(fileName))
        {
            if (fileName.Contains("EAC3", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("EAC-3", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("DD+", StringComparison.OrdinalIgnoreCase))
                return "EAC3";
            return "AC3";
        }
        if (AudioAacRegex().IsMatch(fileName)) return "AAC";
        if (AudioFlacRegex().IsMatch(fileName)) return "FLAC";
        if (AudioOpusRegex().IsMatch(fileName)) return "Opus";
        return "Unknown";
    }

    // ── ffprobe codec mapping ────────────────────────────────────

    private static string MapVideoCodec(string codecName) => codecName.ToLowerInvariant() switch
    {
        "hevc" or "h265" => "HEVC",
        "h264" or "avc" => "H.264",
        "av1" => "AV1",
        "vp9" => "VP9",
        "mpeg2video" => "MPEG-2",
        _ => codecName.ToUpperInvariant()
    };

    private static string MapAudioCodec(string codecName) => codecName.ToLowerInvariant() switch
    {
        "truehd" => "TrueHD",
        "dts" => "DTS",
        "eac3" or "ec3" => "EAC3",
        "ac3" => "AC3",
        "aac" => "AAC",
        "flac" => "FLAC",
        "opus" => "Opus",
        "mp3" or "mp3float" => "MP3",
        "pcm_s16le" or "pcm_s24le" or "pcm_s32le" => "PCM",
        _ => codecName.ToUpperInvariant()
    };

    private static string MapResolution(int height) => height switch
    {
        >= 2160 => "UHD",
        >= 1440 => "QHD",
        >= 1080 => "1080p",
        >= 720 => "720p",
        _ => "SD"
    };

    private static string MapAudioChannels(int channels, string codec)
    {
        var hasAtmos = codec.Contains("Atmos", StringComparison.OrdinalIgnoreCase);
        return channels switch
        {
            >= 8 => hasAtmos ? "7.1 Atmos" : "7.1",
            >= 6 => "5.1",
            >= 2 => "2.0 Stereo",
            1 => "1.0 Mono",
            _ => "2.0 Stereo"
        };
    }

    private static string MapChannelLayout(string layout) => layout.ToLowerInvariant() switch
    {
        "7.1" or "7.1(side)" => "7.1",
        "5.1" or "5.1(side)" => "5.1",
        "stereo" => "2.0 Stereo",
        "mono" => "1.0 Mono",
        _ => "2.0 Stereo"
    };
}
