namespace MediaMatch.CLI.Infrastructure;

/// <summary>
/// Scans directories for media files using known extensions.
/// </summary>
internal static class MediaFileScanner
{
    private static readonly HashSet<string> MediaExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".flv", ".mov", ".m4v",
            ".mpg", ".mpeg", ".ts", ".m2ts", ".vob", ".webm", ".ogv",
            ".srt", ".sub", ".ssa", ".ass", ".idx", ".vtt",
            ".mp3", ".flac", ".m4a", ".ogg", ".wav", ".wma", ".aac", ".opus",
        };

    public static IReadOnlyList<string> Scan(string path, bool recursive)
    {
        if (File.Exists(path))
            return [path];

        if (!Directory.Exists(path))
            return [];

        var option = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        return Directory.EnumerateFiles(path, "*.*", option)
            .Where(f => MediaExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
