namespace MediaMatch.Core.Models;

/// <summary>
/// Complete raw media information extracted from a file via ffprobe/mediainfo.
/// Provides access to ALL properties per stream, enabling expressions like
/// {video[0].DisplayAspectRatioString} and {audio[0].FormatProfile}.
/// </summary>
public sealed class MediaInfoResult
{
    /// <summary>Full path of the analyzed file.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Container-level / general properties (format, duration, bitrate, etc.).</summary>
    public IReadOnlyDictionary<string, string> General { get; init; } = new Dictionary<string, string>();

    /// <summary>Per-video-stream properties (codec, resolution, HDR, bit depth, etc.).</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> VideoStreams { get; init; } = [];

    /// <summary>Per-audio-stream properties (codec, channels, language, etc.).</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> AudioStreams { get; init; } = [];

    /// <summary>Per-subtitle/text-stream properties (codec, language, etc.).</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> TextStreams { get; init; } = [];

    /// <summary>Total number of streams across all types.</summary>
    public int StreamCount =>
        VideoStreams.Count + AudioStreams.Count + TextStreams.Count;

    /// <summary>
    /// Gets a flat list of all properties across all streams, prefixed by stream type.
    /// Useful for UI display and clipboard export.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> GetAllProperties()
    {
        foreach (var kv in General)
            yield return new($"General.{kv.Key}", kv.Value);

        for (var i = 0; i < VideoStreams.Count; i++)
            foreach (var kv in VideoStreams[i])
                yield return new($"Video[{i}].{kv.Key}", kv.Value);

        for (var i = 0; i < AudioStreams.Count; i++)
            foreach (var kv in AudioStreams[i])
                yield return new($"Audio[{i}].{kv.Key}", kv.Value);

        for (var i = 0; i < TextStreams.Count; i++)
            foreach (var kv in TextStreams[i])
                yield return new($"Text[{i}].{kv.Key}", kv.Value);
    }

    /// <summary>
    /// Exports all properties as formatted text for clipboard/file export.
    /// </summary>
    public string ExportAsText()
    {
        var lines = new List<string> { $"File: {FilePath}", "" };

        AppendSection(lines, "General", General);

        for (var i = 0; i < VideoStreams.Count; i++)
            AppendSection(lines, $"Video #{i + 1}", VideoStreams[i]);

        for (var i = 0; i < AudioStreams.Count; i++)
            AppendSection(lines, $"Audio #{i + 1}", AudioStreams[i]);

        for (var i = 0; i < TextStreams.Count; i++)
            AppendSection(lines, $"Text #{i + 1}", TextStreams[i]);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendSection(List<string> lines, string header, IReadOnlyDictionary<string, string> props)
    {
        lines.Add($"--- {header} ---");
        foreach (var kv in props)
            lines.Add($"  {kv.Key}: {kv.Value}");
        lines.Add("");
    }
}
