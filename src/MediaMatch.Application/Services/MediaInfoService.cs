using System.Diagnostics;
using System.Text.Json;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Extracts complete raw media properties from files by running ffprobe.
/// Parses ALL properties from every stream into dictionaries, enabling
/// expression bindings like {video[0].DisplayAspectRatioString}.
/// </summary>
public sealed class MediaInfoService : IMediaInfoService
{
    private readonly ILogger<MediaInfoService> _logger;

    public MediaInfoService(ILogger<MediaInfoService>? logger = null)
    {
        _logger = logger ?? NullLogger<MediaInfoService>.Instance;
    }

    public async Task<MediaInfoResult?> GetMediaInfoAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("File not found: {FilePath}", filePath);
            return null;
        }

        var json = await RunFfprobeAsync(filePath, ct);
        if (json is null)
            return null;

        return ParseFfprobeJson(filePath, json);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> RunFfprobeAsync(string filePath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -print_format json -show_streams -show_format \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("ffprobe returned exit code {Code} for {File}", process.ExitCode, filePath);
                return null;
            }

            return output;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            _logger.LogDebug("ffprobe not found on PATH");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ffprobe execution failed for {File}", filePath);
            return null;
        }
    }

    private MediaInfoResult? ParseFfprobeJson(string filePath, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parse general/format properties
            var general = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("format", out var format))
            {
                FlattenJsonObject(format, general);
            }

            // Parse streams
            var videoStreams = new List<Dictionary<string, string>>();
            var audioStreams = new List<Dictionary<string, string>>();
            var textStreams = new List<Dictionary<string, string>>();

            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    var codecType = stream.TryGetProperty("codec_type", out var ct)
                        ? ct.GetString() : null;

                    var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    FlattenJsonObject(stream, props);

                    switch (codecType)
                    {
                        case "video":
                            videoStreams.Add(props);
                            break;
                        case "audio":
                            audioStreams.Add(props);
                            break;
                        case "subtitle":
                            textStreams.Add(props);
                            break;
                        default:
                            // Attachment or data streams — include in general
                            foreach (var kv in props)
                                general.TryAdd($"stream_{codecType}_{kv.Key}", kv.Value);
                            break;
                    }
                }
            }

            return new MediaInfoResult
            {
                FilePath = filePath,
                General = general,
                VideoStreams = videoStreams,
                AudioStreams = audioStreams,
                TextStreams = textStreams
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse ffprobe JSON for {File}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Flattens a JSON object into a string dictionary, converting nested objects
    /// and arrays into dot-separated keys (e.g., "tags.title", "disposition.default").
    /// </summary>
    private static void FlattenJsonObject(JsonElement element, Dictionary<string, string> dict, string prefix = "")
    {
        if (element.ValueKind != JsonValueKind.Object) return;

        foreach (var prop in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? ToPascalCase(prop.Name) : $"{prefix}.{ToPascalCase(prop.Name)}";

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    dict[key] = prop.Value.GetString() ?? "";
                    break;
                case JsonValueKind.Number:
                    dict[key] = prop.Value.GetRawText();
                    break;
                case JsonValueKind.True:
                    dict[key] = "true";
                    break;
                case JsonValueKind.False:
                    dict[key] = "false";
                    break;
                case JsonValueKind.Object:
                    FlattenJsonObject(prop.Value, dict, key);
                    break;
                case JsonValueKind.Array:
                    // Store array as comma-separated string
                    var items = new List<string>();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                            items.Add(item.GetString() ?? "");
                        else
                            items.Add(item.GetRawText());
                    }
                    dict[key] = string.Join(", ", items);
                    break;
            }
        }
    }

    /// <summary>
    /// Converts snake_case ffprobe property names to PascalCase for expression compatibility.
    /// e.g., "codec_name" → "CodecName", "display_aspect_ratio" → "DisplayAspectRatio"
    /// </summary>
    private static string ToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase)) return snakeCase;

        var parts = snakeCase.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        }

        return string.Join("", parts);
    }
}
