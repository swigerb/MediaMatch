using FluentAssertions;
using MediaMatch.Core.Models;

namespace MediaMatch.Core.Tests.Models;

public sealed class MultiEpisodeMatchTests
{
    [Fact]
    public void FromEpisodes_SingleEpisode_Works()
    {
        var episodes = new List<Episode>
        {
            new("Show", 1, 5, "Episode Five")
        };

        var match = MultiEpisodeMatch.FromEpisodes(episodes);
        match.SeriesName.Should().Be("Show");
        match.Season.Should().Be(1);
        match.StartEpisode.Should().Be(5);
        match.EndEpisode.Should().Be(5);
        match.EpisodeCount.Should().Be(1);
        match.MergedTitle.Should().Be("Episode Five");
    }

    [Fact]
    public void FromEpisodes_MultipleEpisodes_MergesTitles()
    {
        var episodes = new List<Episode>
        {
            new("Show", 1, 1, "Pilot"),
            new("Show", 1, 2, "Second Episode"),
            new("Show", 1, 3, "Third Episode")
        };

        var match = MultiEpisodeMatch.FromEpisodes(episodes);
        match.SeriesName.Should().Be("Show");
        match.Season.Should().Be(1);
        match.StartEpisode.Should().Be(1);
        match.EndEpisode.Should().Be(3);
        match.EpisodeCount.Should().Be(3);
        match.MergedTitle.Should().Be("Pilot & Second Episode & Third Episode");
    }

    [Fact]
    public void FromEpisodes_SortsEpisodeNumbers()
    {
        var episodes = new List<Episode>
        {
            new("Show", 1, 3, "C"),
            new("Show", 1, 1, "A"),
            new("Show", 1, 2, "B")
        };

        var match = MultiEpisodeMatch.FromEpisodes(episodes);
        match.EpisodeNumbers.Should().Equal(1, 2, 3);
        match.StartEpisode.Should().Be(1);
        match.EndEpisode.Should().Be(3);
    }

    [Fact]
    public void FromEpisodes_EmptyList_Throws()
    {
        var act = () => MultiEpisodeMatch.FromEpisodes(Array.Empty<Episode>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromEpisodes_SkipsEmptyTitles()
    {
        var episodes = new List<Episode>
        {
            new("Show", 1, 1, "First"),
            new("Show", 1, 2, ""),
            new("Show", 1, 3, "Third")
        };

        var match = MultiEpisodeMatch.FromEpisodes(episodes);
        match.MergedTitle.Should().Be("First & Third");
    }

    [Fact]
    public void EpisodeCount_ReflectsActualCount()
    {
        var match = new MultiEpisodeMatch(
            "Show", 1, 5, 8, new List<int> { 5, 6, 7, 8 }, "Combined");
        match.EpisodeCount.Should().Be(4);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var nums = new List<int> { 1, 2 };
        var m1 = new MultiEpisodeMatch("Show", 1, 1, 2, nums, "A & B");
        var m2 = new MultiEpisodeMatch("Show", 1, 1, 2, nums, "A & B");
        m1.Should().Be(m2);
    }
}

public sealed class MatchSuggestionTests
{
    [Fact]
    public void MatchSuggestion_RecordConstruction()
    {
        var suggestion = new MatchSuggestion("TMDb", 0.75, "Test Movie", 2024, "Overview", "12345");
        suggestion.ProviderName.Should().Be("TMDb");
        suggestion.Confidence.Should().Be(0.75);
        suggestion.Title.Should().Be("Test Movie");
        suggestion.Year.Should().Be(2024);
        suggestion.MetadataSummary.Should().Be("Overview");
        suggestion.ProviderId.Should().Be("12345");
    }

    [Fact]
    public void MatchSuggestion_NullOptionals()
    {
        var suggestion = new MatchSuggestion("Provider", 0.5, "Title", null, null, null);
        suggestion.Year.Should().BeNull();
        suggestion.MetadataSummary.Should().BeNull();
        suggestion.ProviderId.Should().BeNull();
    }

    [Fact]
    public void MatchSuggestion_RecordEquality()
    {
        var s1 = new MatchSuggestion("P", 0.8, "T", 2024, "S", "1");
        var s2 = new MatchSuggestion("P", 0.8, "T", 2024, "S", "1");
        s1.Should().Be(s2);
    }
}

public sealed class MediaTechnicalInfoTests
{
    [Fact]
    public void RecordConstruction_AllFields()
    {
        var info = new MediaTechnicalInfo("7.1 Atmos", "DoVi P5", "HDR10+", "UHD", "10bit", "HEVC", "TrueHD Atmos");
        info.AudioChannels.Should().Be("7.1 Atmos");
        info.DolbyVision.Should().Be("DoVi P5");
        info.HdrFormat.Should().Be("HDR10+");
        info.Resolution.Should().Be("UHD");
        info.BitDepth.Should().Be("10bit");
        info.VideoCodec.Should().Be("HEVC");
        info.AudioCodec.Should().Be("TrueHD Atmos");
    }

    [Fact]
    public void Unknown_ReturnsDefaults()
    {
        var unknown = MediaTechnicalInfo.Unknown;
        unknown.AudioChannels.Should().Be("2.0 Stereo");
        unknown.DolbyVision.Should().BeNull();
        unknown.HdrFormat.Should().BeNull();
        unknown.Resolution.Should().Be("SD");
        unknown.BitDepth.Should().Be("8bit");
        unknown.VideoCodec.Should().Be("Unknown");
        unknown.AudioCodec.Should().Be("Unknown");
    }

    [Fact]
    public void RecordEquality()
    {
        var a = new MediaTechnicalInfo("5.1", null, "HDR10", "1080p", "8bit", "H.264", "AAC");
        var b = new MediaTechnicalInfo("5.1", null, "HDR10", "1080p", "8bit", "H.264", "AAC");
        a.Should().Be(b);
    }

    [Fact]
    public void RecordInequality()
    {
        var a = new MediaTechnicalInfo("5.1", null, "HDR10", "1080p", "8bit", "H.264", "AAC");
        var b = new MediaTechnicalInfo("7.1", null, "HDR10", "1080p", "8bit", "HEVC", "AAC");
        a.Should().NotBe(b);
    }
}

public sealed class MusicTrackModelTests
{
    [Fact]
    public void DisplayArtist_WithFeatured_IncludesFeatured()
    {
        var track = new MusicTrack("Song", "Main",
            FeaturedArtists: new List<string> { "A", "B" });
        track.DisplayArtist.Should().Be("Main feat. A, B");
    }

    [Fact]
    public void DisplayArtist_WithoutFeatured_JustArtist()
    {
        var track = new MusicTrack("Song", "Solo");
        track.DisplayArtist.Should().Be("Solo");
    }

    [Fact]
    public void DisplayArtist_EmptyFeaturedList_JustArtist()
    {
        var track = new MusicTrack("Song", "Artist",
            FeaturedArtists: new List<string>());
        track.DisplayArtist.Should().Be("Artist");
    }

    [Fact]
    public void DisplayArtist_NullFeatured_JustArtist()
    {
        var track = new MusicTrack("Song", "Artist",
            FeaturedArtists: null);
        track.DisplayArtist.Should().Be("Artist");
    }

    [Fact]
    public void AllOptionalFields_DefaultToNull()
    {
        var track = new MusicTrack("Song", "Artist");
        track.Album.Should().BeNull();
        track.AlbumArtist.Should().BeNull();
        track.TrackNumber.Should().BeNull();
        track.DiscNumber.Should().BeNull();
        track.TotalDiscs.Should().BeNull();
        track.Genre.Should().BeNull();
        track.Year.Should().BeNull();
        track.MusicBrainzId.Should().BeNull();
        track.Duration.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_SameValues()
    {
        var t1 = new MusicTrack("Song", "Artist", Album: "Album", Year: 2024);
        var t2 = new MusicTrack("Song", "Artist", Album: "Album", Year: 2024);
        t1.Should().Be(t2);
    }

    [Fact]
    public void FullConstruction_AllFields()
    {
        var track = new MusicTrack(
            Title: "Song",
            Artist: "Artist",
            Album: "Album",
            AlbumArtist: "Band",
            TrackNumber: 5,
            DiscNumber: 2,
            TotalDiscs: 3,
            Genre: "Rock",
            Year: 2024,
            FeaturedArtists: new List<string> { "Guest" },
            MusicBrainzId: "mb-123",
            Duration: 300);

        track.Title.Should().Be("Song");
        track.AlbumArtist.Should().Be("Band");
        track.TotalDiscs.Should().Be(3);
        track.MusicBrainzId.Should().Be("mb-123");
    }
}
