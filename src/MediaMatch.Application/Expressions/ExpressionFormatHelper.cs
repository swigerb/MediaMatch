using System.Text.RegularExpressions;
using Scriban.Runtime;

namespace MediaMatch.Application.Expressions;

/// <summary>
/// Custom Scriban functions available in expressions.
/// Registered as a global "mm" object: {{mm.pad e 2}}
/// </summary>
public sealed class ExpressionFormatHelper : ScriptObject
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>Zero-pad a number: {{mm.pad e 2}} → "02"</summary>
    public static string Pad(int value, int width) => value.ToString().PadLeft(width, '0');

    /// <summary>Upper case first letter: {{mm.upper_first "hello"}} → "Hello"</summary>
    public static string UpperFirst(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    /// <summary>Clean file name by removing invalid path characters.</summary>
    public static string CleanFilename(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var span = text.AsSpan();
        var result = new char[span.Length];
        int pos = 0;
        foreach (var c in span)
        {
            if (Array.IndexOf(InvalidFileNameChars, c) < 0)
                result[pos++] = c;
        }
        return new string(result, 0, pos);
    }

    /// <summary>Return first non-null/non-empty value: {{mm.coalesce title "Unknown"}}</summary>
    public static string Coalesce(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrEmpty(v)) return v;
        }
        return string.Empty;
    }

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Replace with regex: {{mm.regex_replace text "pattern" "replacement"}}</summary>
    public static string RegexReplace(string? text, string pattern, string replacement)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        try
        {
            return Regex.Replace(text, pattern, replacement, RegexOptions.None, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return text;
        }
        catch (ArgumentException)
        {
            return text;
        }
    }
}
