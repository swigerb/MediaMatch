using System.Text.RegularExpressions;

namespace MediaMatch.Application.Matching;

/// <summary>
/// Provides text normalization utilities for media name comparison.
/// </summary>
public static partial class Normalization
{
    [GeneratedRegex(@"[\p{P}\p{S}]+", RegexOptions.Compiled)]
    private static partial Regex PunctuationPattern();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"[\[\(]\d{3,4}[pi][\]\)]|[\[\(]\d{4}[\]\)]|[\[\(][^\[\]()]*[\]\)]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ReleaseInfoPattern();

    /// <summary>
    /// Normalizes a media name by lowercasing, replacing punctuation with spaces, and collapsing whitespace.
    /// </summary>
    /// <param name="name">The name to normalize.</param>
    /// <returns>The normalized name, or <see cref="string.Empty"/> if the input is null or whitespace.</returns>
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.ToLowerInvariant();
        result = PunctuationPattern().Replace(result, " ");
        result = WhitespacePattern().Replace(result, " ");
        return result.Trim();
    }

    /// <summary>
    /// Strips the file extension from a file name.
    /// </summary>
    /// <param name="fileName">The file name to process.</param>
    /// <returns>The file name without its extension, or <see cref="string.Empty"/> if the input is null or whitespace.</returns>
    public static string StripExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var name = Path.GetFileNameWithoutExtension(fileName);
        return name;
    }

    /// <summary>
    /// Removes bracketed release information such as resolution, year, and fansub tags from a file name.
    /// </summary>
    /// <param name="fileName">The file name to process.</param>
    /// <returns>The file name with release info tags removed, or <see cref="string.Empty"/> if the input is null or whitespace.</returns>
    public static string StripReleaseInfo(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        return ReleaseInfoPattern().Replace(fileName, " ").Trim();
    }
}
