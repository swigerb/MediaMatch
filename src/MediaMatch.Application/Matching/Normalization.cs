using System.Text.RegularExpressions;

namespace MediaMatch.Application.Matching;

public static partial class Normalization
{
    [GeneratedRegex(@"[\p{P}\p{S}]+", RegexOptions.Compiled)]
    private static partial Regex PunctuationPattern();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"[\[\(]\d{3,4}[pi][\]\)]|[\[\(]\d{4}[\]\)]|[\[\(][^\[\]()]*[\]\)]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ReleaseInfoPattern();

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.ToLowerInvariant();
        result = PunctuationPattern().Replace(result, " ");
        result = WhitespacePattern().Replace(result, " ");
        return result.Trim();
    }

    public static string StripExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var name = Path.GetFileNameWithoutExtension(fileName);
        return name;
    }

    public static string StripReleaseInfo(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        return ReleaseInfoPattern().Replace(fileName, " ").Trim();
    }
}
