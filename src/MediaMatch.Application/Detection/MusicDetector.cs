using System.Text.RegularExpressions;
using MediaMatch.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Detection;

/// <summary>
/// Detects music files and extracts embedded tag metadata via simple header parsing.
/// Handles multi-disc detection and featured artist extraction.
/// </summary>
public sealed partial class MusicDetector
{
    private static readonly HashSet<string> MusicExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".m4a", ".ogg", ".wav", ".wma", ".aac", ".opus"
        };

    private readonly ILogger<MusicDetector> _logger;

    public MusicDetector(ILogger<MusicDetector>? logger = null)
    {
        _logger = logger ?? NullLogger<MusicDetector>.Instance;
    }

    /// <summary>Returns true if the file extension is a known music format.</summary>
    public static bool IsMusicFile(string filePath) =>
        MusicExtensions.Contains(Path.GetExtension(filePath));

    /// <summary>
    /// Extract basic metadata from embedded tags (ID3v2 for MP3, Vorbis for FLAC/OGG).
    /// Falls back to filename parsing if tags are empty.
    /// </summary>
    public MusicTrack? DetectFromFile(string filePath)
    {
        if (!File.Exists(filePath) || !IsMusicFile(filePath))
            return null;

        try
        {
            var tags = ReadBasicTags(filePath);
            if (tags is not null)
            {
                var (artist, featured) = ExtractFeaturedArtists(tags.Artist ?? string.Empty, tags.Title ?? string.Empty);
                var discNumber = tags.DiscNumber ?? DetectDiscNumber(filePath);

                return new MusicTrack(
                    Title: CleanTitle(tags.Title ?? Path.GetFileNameWithoutExtension(filePath)),
                    Artist: artist,
                    Album: tags.Album,
                    AlbumArtist: tags.AlbumArtist,
                    TrackNumber: tags.TrackNumber,
                    DiscNumber: discNumber,
                    TotalDiscs: DetectTotalDiscs(filePath),
                    Genre: tags.Genre,
                    Year: tags.Year,
                    FeaturedArtists: featured.Count > 0 ? featured : null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read tags from {File}", filePath);
        }

        // Fallback: parse from filename
        return DetectFromFilename(filePath);
    }

    /// <summary>
    /// Parse artist and title from a filename like "Artist - Title.mp3".
    /// </summary>
    public MusicTrack? DetectFromFilename(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // Pattern: "01 - Title", "01. Title", "Track - Artist - Title"
        var trackMatch = TrackNumberRegex().Match(fileName);
        int? trackNumber = null;
        var remaining = fileName;

        if (trackMatch.Success)
        {
            trackNumber = int.Parse(trackMatch.Groups[1].Value);
            remaining = trackMatch.Groups[2].Value.Trim();
        }

        // Try "Artist - Title" split
        var dashIdx = remaining.IndexOf(" - ", StringComparison.Ordinal);
        string artist, title;

        if (dashIdx > 0)
        {
            artist = remaining[..dashIdx].Trim();
            title = remaining[(dashIdx + 3)..].Trim();
        }
        else
        {
            artist = string.Empty;
            title = remaining;
        }

        var (cleanArtist, featured) = ExtractFeaturedArtists(artist, title);
        title = CleanTitle(title);

        var discNumber = DetectDiscNumber(filePath);

        return new MusicTrack(
            Title: title,
            Artist: cleanArtist,
            TrackNumber: trackNumber,
            DiscNumber: discNumber,
            TotalDiscs: DetectTotalDiscs(filePath),
            FeaturedArtists: featured.Count > 0 ? featured : null);
    }

    /// <summary>
    /// Extract featured artists from title/artist strings.
    /// Handles patterns like "feat.", "ft.", "featuring", "with", "&amp;".
    /// </summary>
    public static (string PrimaryArtist, List<string> Featured) ExtractFeaturedArtists(string artist, string title)
    {
        var featured = new List<string>();
        var primaryArtist = artist;

        // Extract from artist field: "Artist feat. Other"
        var artistFeatMatch = FeaturedArtistRegex().Match(artist);
        if (artistFeatMatch.Success)
        {
            primaryArtist = artist[..artistFeatMatch.Index].Trim();
            var featStr = artistFeatMatch.Groups[1].Value.Trim();
            featured.AddRange(SplitArtists(featStr));
        }

        // Extract from title: "Song (feat. Other)" or "Song [ft. Other]"
        var titleFeatMatch = FeaturedInTitleRegex().Match(title);
        if (titleFeatMatch.Success)
        {
            var featStr = titleFeatMatch.Groups[1].Value.Trim();
            featured.AddRange(SplitArtists(featStr));
        }

        return (primaryArtist, featured.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// Detect disc number from parent folder patterns like CD01, Disc 2, etc.
    /// </summary>
    public static int? DetectDiscNumber(string filePath)
    {
        var parentDir = Path.GetFileName(Path.GetDirectoryName(filePath));
        if (string.IsNullOrWhiteSpace(parentDir))
            return null;

        var match = DiscFolderRegex().Match(parentDir);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var disc))
            return disc;

        return null;
    }

    /// <summary>
    /// Detect total disc count by scanning parent folder for CD/Disc subfolder siblings.
    /// </summary>
    public static int? DetectTotalDiscs(string filePath)
    {
        var parentDir = Path.GetDirectoryName(filePath);
        if (parentDir is null) return null;

        var grandParent = Path.GetDirectoryName(parentDir);
        if (grandParent is null || !Directory.Exists(grandParent))
            return null;

        try
        {
            var discFolders = Directory.GetDirectories(grandParent)
                .Select(d => Path.GetFileName(d))
                .Count(name => DiscFolderRegex().IsMatch(name ?? string.Empty));

            return discFolders > 1 ? discFolders : null;
        }
        catch
        {
            return null;
        }
    }

    // ── Tag reading ─────────────────────────────────────────────

    private BasicTags? ReadBasicTags(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".mp3" => ReadId3v2Tags(filePath),
            ".flac" or ".ogg" or ".opus" => ReadVorbisCommentTags(filePath),
            _ => null
        };
    }

    private BasicTags? ReadId3v2Tags(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var header = new byte[10];
            if (stream.Read(header, 0, 10) < 10) return null;

            // Check for ID3v2 header: "ID3"
            if (header[0] != 'I' || header[1] != 'D' || header[2] != '3')
                return null;

            var size = DecodeId3Size(header, 6);
            if (size <= 0 || size > 10 * 1024 * 1024) return null; // sanity check 10MB

            var tagData = new byte[Math.Min(size, 8192)]; // read up to 8KB of tags
            var bytesRead = stream.Read(tagData, 0, tagData.Length);

            var tags = new BasicTags();
            var pos = 0;

            while (pos + 10 < bytesRead)
            {
                var frameId = System.Text.Encoding.ASCII.GetString(tagData, pos, 4);
                if (frameId[0] == '\0') break;

                var frameSize = (tagData[pos + 4] << 24) | (tagData[pos + 5] << 16) |
                                (tagData[pos + 6] << 8) | tagData[pos + 7];

                pos += 10;
                if (frameSize <= 0 || pos + frameSize > bytesRead) break;

                var value = ReadId3TextFrame(tagData, pos, frameSize);

                switch (frameId)
                {
                    case "TIT2": tags.Title = value; break;
                    case "TPE1": tags.Artist = value; break;
                    case "TALB": tags.Album = value; break;
                    case "TPE2": tags.AlbumArtist = value; break;
                    case "TRCK":
                        if (value?.Contains('/') == true)
                            value = value.Split('/')[0];
                        if (int.TryParse(value, out var track))
                            tags.TrackNumber = track;
                        break;
                    case "TPOS":
                        if (value?.Contains('/') == true)
                            value = value.Split('/')[0];
                        if (int.TryParse(value, out var disc))
                            tags.DiscNumber = disc;
                        break;
                    case "TCON": tags.Genre = value; break;
                    case "TDRC" or "TYER":
                        if (value?.Length >= 4 && int.TryParse(value[..4], out var year))
                            tags.Year = year;
                        break;
                }

                pos += frameSize;
            }

            return tags.Title is not null || tags.Artist is not null ? tags : null;
        }
        catch
        {
            return null;
        }
    }

    private static BasicTags? ReadVorbisCommentTags(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            // Read enough to find Vorbis comments — simplified approach
            var buffer = new byte[Math.Min(stream.Length, 65536)];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            var tags = new BasicTags();

            // Simple Vorbis comment extraction — look for key=value patterns
            tags.Title = ExtractVorbisTag(buffer, bytesRead, "TITLE");
            tags.Artist = ExtractVorbisTag(buffer, bytesRead, "ARTIST");
            tags.Album = ExtractVorbisTag(buffer, bytesRead, "ALBUM");
            tags.AlbumArtist = ExtractVorbisTag(buffer, bytesRead, "ALBUMARTIST");
            tags.Genre = ExtractVorbisTag(buffer, bytesRead, "GENRE");

            var trackStr = ExtractVorbisTag(buffer, bytesRead, "TRACKNUMBER");
            if (int.TryParse(trackStr, out var track)) tags.TrackNumber = track;

            var discStr = ExtractVorbisTag(buffer, bytesRead, "DISCNUMBER");
            if (int.TryParse(discStr, out var disc)) tags.DiscNumber = disc;

            var dateStr = ExtractVorbisTag(buffer, bytesRead, "DATE");
            if (dateStr?.Length >= 4 && int.TryParse(dateStr[..4], out var year))
                tags.Year = year;

            return tags.Title is not null || tags.Artist is not null ? tags : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractVorbisTag(byte[] buffer, int length, string tagName)
    {
        var searchBytes = System.Text.Encoding.ASCII.GetBytes(tagName + "=");
        for (int i = 0; i < length - searchBytes.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < searchBytes.Length; j++)
            {
                var b = buffer[i + j];
                var s = searchBytes[j];
                // Case-insensitive
                if (b != s && b != (s >= 'A' && s <= 'Z' ? s + 32 : s))
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                var start = i + searchBytes.Length;
                var end = start;
                while (end < length && buffer[end] != 0 && buffer[end] != '\n' && buffer[end] != '\r')
                    end++;

                if (end > start)
                    return System.Text.Encoding.UTF8.GetString(buffer, start, end - start);
            }
        }

        return null;
    }

    private static int DecodeId3Size(byte[] header, int offset)
    {
        return (header[offset] << 21) | (header[offset + 1] << 14) |
               (header[offset + 2] << 7) | header[offset + 3];
    }

    private static string? ReadId3TextFrame(byte[] data, int offset, int length)
    {
        if (length <= 1) return null;

        var encoding = data[offset];
        var textStart = offset + 1;
        var textLength = length - 1;

        return encoding switch
        {
            0 => System.Text.Encoding.Latin1.GetString(data, textStart, textLength).TrimEnd('\0'),
            1 or 2 => System.Text.Encoding.Unicode.GetString(data, textStart, textLength).TrimEnd('\0'),
            3 => System.Text.Encoding.UTF8.GetString(data, textStart, textLength).TrimEnd('\0'),
            _ => System.Text.Encoding.Latin1.GetString(data, textStart, textLength).TrimEnd('\0')
        };
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string CleanTitle(string title)
    {
        // Remove featured artist patterns from title
        return FeaturedInTitleRegex().Replace(title, string.Empty).Trim();
    }

    private static IEnumerable<string> SplitArtists(string artists)
    {
        return artists.Split([',', '&', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(a => !string.IsNullOrWhiteSpace(a));
    }

    // ── Regex patterns ──────────────────────────────────────────

    [GeneratedRegex(@"^\s*(\d{1,3})[\.\-\s]+(.+)$")]
    private static partial Regex TrackNumberRegex();

    [GeneratedRegex(@"\b(?:feat\.?|ft\.?|featuring|with)\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex FeaturedArtistRegex();

    [GeneratedRegex(@"[\(\[]\s*(?:feat\.?|ft\.?|featuring|with)\s+([^\)\]]+)[\)\]]", RegexOptions.IgnoreCase)]
    private static partial Regex FeaturedInTitleRegex();

    [GeneratedRegex(@"(?:CD|Disc|DISC|disk)\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DiscFolderRegex();

    // ── Inner types ─────────────────────────────────────────────

    private sealed class BasicTags
    {
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? AlbumArtist { get; set; }
        public int? TrackNumber { get; set; }
        public int? DiscNumber { get; set; }
        public string? Genre { get; set; }
        public int? Year { get; set; }
    }
}
