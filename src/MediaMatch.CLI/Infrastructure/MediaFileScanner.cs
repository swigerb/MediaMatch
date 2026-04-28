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

    /// <summary>
    /// Scans the specified path for media files using known extensions.
    /// </summary>
    /// <param name="path">A file path or directory to scan.</param>
    /// <param name="recursive">When <c>true</c>, scan subdirectories recursively.</param>
    /// <returns>A sorted list of media file paths.</returns>
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
