using System.Text.RegularExpressions;
using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

public sealed partial class ReleaseInfoParser
{
    // ── Season / Episode ────────────────────────────────────────────────

    // S01E02, S1E2, S01E02E03, S01E02-E05
    [GeneratedRegex(@"[Ss](\d{1,2})\s*[Ee](\d{1,3})(?:\s*-?\s*[Ee](\d{1,3}))?", RegexOptions.Compiled)]
    private static partial Regex SxxExxRegex();

    // 1x02, 01x02
    [GeneratedRegex(@"(\d{1,2})[xX](\d{2,3})", RegexOptions.Compiled)]
    private static partial Regex NxNNRegex();

    // Season 1 Episode 2
    [GeneratedRegex(@"[Ss]eason\s+(\d{1,2})\s+[Ee]pisode\s+(\d{1,3})", RegexOptions.Compiled)]
    private static partial Regex SeasonEpisodeWordRegex();

    // Episode 42 (absolute numbering)
    [GeneratedRegex(@"(?<![Ss]eason\s{0,4})[Ee](?:pisode)?\s*(\d{1,4})(?!\s*[xX])", RegexOptions.Compiled)]
    private static partial Regex AbsoluteEpisodeRegex();

    // ── Quality ─────────────────────────────────────────────────────────

    [GeneratedRegex(@"(?:2160[pi]|4[Kk]|UHD)", RegexOptions.Compiled)]
    private static partial Regex Quality4KRegex();

    [GeneratedRegex(@"1080[pi]", RegexOptions.Compiled)]
    private static partial Regex Quality1080Regex();

    [GeneratedRegex(@"720[pi]", RegexOptions.Compiled)]
    private static partial Regex Quality720Regex();

    [GeneratedRegex(@"(?:480[pi]|576[pi]|\bSD\b)", RegexOptions.Compiled)]
    private static partial Regex QualitySDRegex();

    [GeneratedRegex(@"8[Kk]|4320[pi]", RegexOptions.Compiled)]
    private static partial Regex Quality8KRegex();

    // ── Video Source ────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(Blu-?Ray|BDRip|BRRip|BDREMUX)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceBluRayRegex();

    [GeneratedRegex(@"\b(WEB-?DL|WEBRip|WEBDL|WEB)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceWebRegex();

    [GeneratedRegex(@"\b(HDTV|PDTV|DSR)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceHDTVRegex();

    [GeneratedRegex(@"\b(DVDRip|DVDR|DVD5|DVD9)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceDVDRegex();

    [GeneratedRegex(@"\b(HDRip)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceHDRipRegex();

    [GeneratedRegex(@"\b(CAM|HDCAM)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceCamRegex();

    [GeneratedRegex(@"\b(TS|TELESYNC|TELECINE|TC)\b", RegexOptions.Compiled)]
    private static partial Regex SourceTeleSyncRegex();

    // ── Video Codec ─────────────────────────────────────────────────────

    [GeneratedRegex(@"\b[xXhH]\.?264\b|AVC", RegexOptions.Compiled)]
    private static partial Regex CodecH264Regex();

    [GeneratedRegex(@"\b[xXhH]\.?265\b|HEVC", RegexOptions.Compiled)]
    private static partial Regex CodecH265Regex();

    [GeneratedRegex(@"\bAV1\b", RegexOptions.Compiled)]
    private static partial Regex CodecAV1Regex();

    [GeneratedRegex(@"\bVP9\b", RegexOptions.Compiled)]
    private static partial Regex CodecVP9Regex();

    // ── Audio Codec ─────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(DTS-HD(?:\.?MA)?|DTS-X|DTS)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioDTSRegex();

    [GeneratedRegex(@"\b(TrueHD|Atmos)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioTrueHDRegex();

    [GeneratedRegex(@"\bAAC\b", RegexOptions.Compiled)]
    private static partial Regex AudioAACRegex();

    [GeneratedRegex(@"\bAC-?3\b|EAC-?3|\bDD5?\.1\b|\bDolby\s?Digital\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioAC3Regex();

    [GeneratedRegex(@"\bFLAC\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioFLACRegex();

    [GeneratedRegex(@"\bMP3\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AudioMP3Regex();

    // ── Release Group ───────────────────────────────────────────────────

    // -GroupName at end (before extension)
    [GeneratedRegex(@"-([A-Za-z0-9]+)(?:\.[a-z]{2,4})?$", RegexOptions.Compiled)]
    private static partial Regex ReleaseGroupDashRegex();

    // [GroupName] at start
    [GeneratedRegex(@"^\[([^\]]+)\]", RegexOptions.Compiled)]
    private static partial Regex ReleaseGroupBracketRegex();

    // ── Year ────────────────────────────────────────────────────────────

    [GeneratedRegex(@"[\(\[\.\s]((?:19|20)\d{2})[\)\]\.\s]", RegexOptions.Compiled)]
    private static partial Regex YearRegex();

    // ── Language ─────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(MULTI|MULTi|DUAL|FRENCH|GERMAN|SPANISH|ITALIAN|PORTUGUESE|RUSSIAN|JAPANESE|KOREAN|CHINESE|HINDI|ARABIC|SWEDISH|NORWEGIAN|DANISH|FINNISH|DUTCH|POLISH|TURKISH|CZECH|HUNGARIAN|ROMANIAN|THAI|VIETNAMESE|INDONESIAN|MALAY|FILIPINO|ENGLISH|ENG|FRE|GER|SPA|ITA|POR|RUS|JPN|KOR|CHI|HIN|ARA)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LanguageRegex();

    // ── Cleaning ────────────────────────────────────────────────────────

    // Known noise tokens used when cleaning a title
    [GeneratedRegex(@"\b(REPACK|PROPER|INTERNAL|LIMITED|EXTENDED|UNRATED|THEATRICAL|DIRECTORS\.?CUT|DC|IMAX|REMUX|RARBG|YTS|YIFY|AMZN|NF|DSNP|HMAX|ATVP|PCOK|BluRay|Blu-?Ray|BDRip|BRRip|WEB-?DL|WEBRip|WEBDL|WEB|HDTV|DVDRip|HDRip|CAM|TELESYNC|TS|REMASTERED|COMPLETE|10bit|8bit|HDR10\+?|HDR|DV|DoVi|Dolby\.?Vision|Atmos|QHD)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NoiseTokenRegex();

    // ── HDR / Dolby Vision / Channels / Bit Depth ────────────────────

    [GeneratedRegex(@"\bHDR10\+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Hdr10PlusRegex();

    [GeneratedRegex(@"\bHDR10\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Hdr10Regex();

    [GeneratedRegex(@"\bHLG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HlgRegex();

    [GeneratedRegex(@"\bHDR\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HdrGenericRegex();

    [GeneratedRegex(@"\bDoVi\s*P(\d)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DoViProfileRegex();

    [GeneratedRegex(@"\b(?:DoVi|DV|Dolby\.?Vision)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DolbyVisionRegex();

    [GeneratedRegex(@"\b(?:7\.1\s*Atmos|7\.1)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Channels71Regex();

    [GeneratedRegex(@"\b5\.1\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Channels51Regex();

    [GeneratedRegex(@"\b10bit\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BitDepth10Regex();

    // ────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────

    public SeasonEpisodeMatch? ParseSeasonEpisode(string fileName)
    {
        var name = StripExtension(fileName);

        var m = SxxExxRegex().Match(name);
        if (m.Success)
        {
            int season = int.Parse(m.Groups[1].Value);
            int episode = int.Parse(m.Groups[2].Value);
            int? endEp = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : null;
            return new SeasonEpisodeMatch(season, episode, endEp);
        }

        m = NxNNRegex().Match(name);
        if (m.Success)
        {
            return new SeasonEpisodeMatch(
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value));
        }

        m = SeasonEpisodeWordRegex().Match(name);
        if (m.Success)
        {
            return new SeasonEpisodeMatch(
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value));
        }

        m = AbsoluteEpisodeRegex().Match(name);
        if (m.Success)
        {
            int abs = int.Parse(m.Groups[1].Value);
            return new SeasonEpisodeMatch(Season: 1, Episode: abs, AbsoluteNumber: abs);
        }

        return null;
    }

    public int? ParseYear(string fileName)
    {
        var m = YearRegex().Match(StripExtension(fileName));
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    public VideoQuality ParseVideoQuality(string fileName)
    {
        var name = StripExtension(fileName);

        if (Quality8KRegex().IsMatch(name))
            return VideoQuality.UHD8K;
        if (Quality4KRegex().IsMatch(name))
            return VideoQuality.UHD4K;
        if (Quality1080Regex().IsMatch(name))
            return VideoQuality.HD1080p;
        if (Quality720Regex().IsMatch(name))
            return VideoQuality.HD720p;
        if (QualitySDRegex().IsMatch(name))
            return VideoQuality.SD;

        return VideoQuality.Unknown;
    }

    public string? ParseReleaseGroup(string fileName)
    {
        var name = StripExtension(fileName);

        var m = ReleaseGroupBracketRegex().Match(name);
        if (m.Success)
            return m.Groups[1].Value.Trim();

        m = ReleaseGroupDashRegex().Match(name);
        if (m.Success)
        {
            string group = m.Groups[1].Value;
            // Exclude common codec/quality tokens that happen to sit at the end
            if (IsKnownToken(group))
                return null;
            return group;
        }

        return null;
    }

    public string? ParseVideoSource(string fileName)
    {
        var name = StripExtension(fileName);

        if (SourceBluRayRegex().IsMatch(name)) return "BluRay";
        if (SourceWebRegex().IsMatch(name))    return "WEB-DL";
        if (SourceHDTVRegex().IsMatch(name))   return "HDTV";
        if (SourceDVDRegex().IsMatch(name))    return "DVD";
        if (SourceHDRipRegex().IsMatch(name))  return "HDRip";
        if (SourceCamRegex().IsMatch(name))    return "CAM";
        if (SourceTeleSyncRegex().IsMatch(name)) return "TELESYNC";

        return null;
    }

    public string? ParseVideoCodec(string fileName)
    {
        var name = StripExtension(fileName);

        if (CodecH265Regex().IsMatch(name)) return "H.265";
        if (CodecH264Regex().IsMatch(name)) return "H.264";
        if (CodecAV1Regex().IsMatch(name))  return "AV1";
        if (CodecVP9Regex().IsMatch(name))  return "VP9";

        return null;
    }

    public string? ParseAudioCodec(string fileName)
    {
        var name = StripExtension(fileName);

        if (AudioTrueHDRegex().IsMatch(name)) return "TrueHD";
        if (AudioDTSRegex().IsMatch(name))    return "DTS";
        if (AudioAC3Regex().IsMatch(name))    return "AC3";
        if (AudioAACRegex().IsMatch(name))    return "AAC";
        if (AudioFLACRegex().IsMatch(name))   return "FLAC";
        if (AudioMP3Regex().IsMatch(name))    return "MP3";

        return null;
    }

    public string CleanTitle(string fileName)
    {
        var name = StripExtension(fileName);

        // Remove bracketed groups at start (e.g. [SubGroup])
        name = ReleaseGroupBracketRegex().Replace(name, "");

        // Remove season/episode markers and everything after
        var seMatch = SxxExxRegex().Match(name);
        if (seMatch.Success)
            name = name[..seMatch.Index];

        seMatch = NxNNRegex().Match(name);
        if (seMatch.Success)
            name = name[..seMatch.Index];

        seMatch = SeasonEpisodeWordRegex().Match(name);
        if (seMatch.Success)
            name = name[..seMatch.Index];

        // Remove year and everything after it (for movies)
        var ym = YearRegex().Match(name);
        if (ym.Success)
            name = name[..ym.Index];

        // Remove quality / source / codec noise
        name = NoiseTokenRegex().Replace(name, "");
        name = Quality4KRegex().Replace(name, "");
        name = Quality1080Regex().Replace(name, "");
        name = Quality720Regex().Replace(name, "");
        name = QualitySDRegex().Replace(name, "");
        name = Quality8KRegex().Replace(name, "");

        // Replace dots / underscores with spaces and collapse whitespace
        name = name.Replace('.', ' ').Replace('_', ' ');
        name = Regex.Replace(name, @"\s{2,}", " ");

        // Remove trailing dash/group fragment
        name = Regex.Replace(name, @"\s*-\s*$", "");

        return name.Trim();
    }

    public string? ParseLanguage(string fileName)
    {
        var m = LanguageRegex().Match(StripExtension(fileName));
        return m.Success ? m.Groups[1].Value.ToUpperInvariant() : null;
    }

    public string? ParseHdrFormat(string fileName)
    {
        var name = StripExtension(fileName);
        if (Hdr10PlusRegex().IsMatch(name)) return "HDR10+";
        if (Hdr10Regex().IsMatch(name)) return "HDR10";
        if (HlgRegex().IsMatch(name)) return "HLG";
        if (HdrGenericRegex().IsMatch(name)) return "HDR10";
        return null;
    }

    public string? ParseDolbyVision(string fileName)
    {
        var name = StripExtension(fileName);
        var profileMatch = DoViProfileRegex().Match(name);
        if (profileMatch.Success) return $"DoVi P{profileMatch.Groups[1].Value}";
        if (DolbyVisionRegex().IsMatch(name)) return "DV";
        return null;
    }

    public string? ParseAudioChannels(string fileName)
    {
        var name = StripExtension(fileName);
        if (Channels71Regex().IsMatch(name))
            return AudioTrueHDRegex().IsMatch(name) ? "7.1 Atmos" : "7.1";
        if (Channels51Regex().IsMatch(name)) return "5.1";
        return null;
    }

    public string? ParseBitDepth(string fileName)
    {
        var name = StripExtension(fileName);
        if (BitDepth10Regex().IsMatch(name)) return "10bit";
        return null;
    }

    public ReleaseInfo Parse(string fileName)
    {
        return new ReleaseInfo(
            OriginalFileName: fileName,
            CleanTitle: CleanTitle(fileName),
            SeasonEpisode: ParseSeasonEpisode(fileName),
            Year: ParseYear(fileName),
            Quality: ParseVideoQuality(fileName),
            VideoSource: ParseVideoSource(fileName),
            VideoCodec: ParseVideoCodec(fileName),
            AudioCodec: ParseAudioCodec(fileName),
            ReleaseGroup: ParseReleaseGroup(fileName),
            Language: ParseLanguage(fileName),
            HdrFormat: ParseHdrFormat(fileName),
            DolbyVision: ParseDolbyVision(fileName),
            AudioChannels: ParseAudioChannels(fileName),
            BitDepth: ParseBitDepth(fileName));
    }

    // ────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────

    private static string StripExtension(string fileName)
    {
        // Only strip known media extensions
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
            return fileName;

        return KnownExtensions.Contains(ext.ToLowerInvariant())
            ? Path.GetFileNameWithoutExtension(fileName)
            : fileName;
    }

    private static bool IsKnownToken(string value)
    {
        return NoiseTokens.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> KnownExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".flv", ".mov", ".m4v",
            ".mpg", ".mpeg", ".ts", ".m2ts", ".vob", ".webm", ".ogv",
            ".srt", ".sub", ".ssa", ".ass", ".idx",
            ".mp3", ".flac", ".ogg", ".m4a", ".wav", ".wma", ".aac",
        };

    private static readonly HashSet<string> NoiseTokens =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "x264", "x265", "h264", "h265", "HEVC", "AVC", "AV1", "VP9",
            "AAC", "AC3", "DTS", "FLAC", "MP3", "TrueHD", "Atmos",
            "720p", "1080p", "2160p", "4K", "UHD", "SD", "480p", "QHD",
            "BluRay", "WEB", "WEBDL", "WEBRip", "HDTV", "DVDRip", "HDRip",
            "REPACK", "PROPER", "INTERNAL", "REMUX", "mkv", "mp4",
            "HDR10", "HDR", "HLG", "DV", "DoVi", "DolbyVision",
            "10bit", "8bit",
        };
}
