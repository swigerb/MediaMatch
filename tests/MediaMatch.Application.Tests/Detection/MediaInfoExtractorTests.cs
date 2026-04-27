using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Detection;

public sealed class MediaInfoExtractorTests
{
    private readonly MediaInfoExtractor _extractor = new();

    // ── Resolution Detection ─────────────────────────────────────
    [Theory]
    [InlineData("Movie.2024.2160p.HEVC.mkv", "UHD")]
    [InlineData("Movie.4K.HDR.mkv", "UHD")]
    [InlineData("Movie.UHD.Blu-Ray.mkv", "UHD")]
    [InlineData("Movie.1080p.BluRay.mkv", "1080p")]
    [InlineData("Movie.720p.WEB-DL.mkv", "720p")]
    [InlineData("Movie.SD.DVDRip.mkv", "SD")]
    [InlineData("Movie.1440p.mkv", "QHD")]
    [InlineData("Movie.QHD.mkv", "QHD")]
    public void ParseFromFileName_Resolution_DetectedCorrectly(string filename, string expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.Resolution.Should().Be(expected);
    }

    // ── Video Codec Detection ────────────────────────────────────
    [Theory]
    [InlineData("Movie.x265.mkv", "HEVC")]
    [InlineData("Movie.HEVC.mkv", "HEVC")]
    [InlineData("Movie.H.265.mkv", "HEVC")]
    [InlineData("Movie.x264.mkv", "H.264")]
    [InlineData("Movie.H.264.mkv", "H.264")]
    [InlineData("Movie.AV1.mkv", "AV1")]
    [InlineData("Movie.VP9.mkv", "VP9")]
    [InlineData("Movie.mkv", "Unknown")]
    public void ParseFromFileName_VideoCodec_DetectedCorrectly(string filename, string expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.VideoCodec.Should().Be(expected);
    }

    // ── Audio Codec Detection ────────────────────────────────────
    [Theory]
    [InlineData("Movie.TrueHD.Atmos.mkv", "TrueHD Atmos")]
    [InlineData("Movie.TrueHD.mkv", "TrueHD")]
    [InlineData("Movie.DTS-HD MA.mkv", "DTS-HD MA")]
    [InlineData("Movie.DTS-X.mkv", "DTS-X")]
    [InlineData("Movie.DTS-HD.mkv", "DTS-HD")]
    [InlineData("Movie.DTS.mkv", "DTS")]
    [InlineData("Movie.EAC3.mkv", "EAC3")]
    [InlineData("Movie.AC3.mkv", "AC3")]
    [InlineData("Movie.AAC.mkv", "AAC")]
    [InlineData("Movie.FLAC.mkv", "FLAC")]
    [InlineData("Movie.OPUS.mkv", "Opus")]
    public void ParseFromFileName_AudioCodec_DetectedCorrectly(string filename, string expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.AudioCodec.Should().Be(expected);
    }

    // ── HDR Format Detection ─────────────────────────────────────
    [Theory]
    [InlineData("Movie.HDR10+remastered.mkv", "HDR10+")]
    [InlineData("Movie.HDR10.mkv", "HDR10")]
    [InlineData("Movie.HLG.mkv", "HLG")]
    [InlineData("Movie.HDR.mkv", "HDR10")]
    [InlineData("Movie.SDR.mkv", null)]
    public void ParseFromFileName_HdrFormat_DetectedCorrectly(string filename, string? expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.HdrFormat.Should().Be(expected);
    }

    // ── Dolby Vision Detection ───────────────────────────────────
    [Theory]
    [InlineData("Movie.DoVi.mkv", "DV")]
    [InlineData("Movie.DV.mkv", "DV")]
    [InlineData("Movie.DolbyVision.mkv", "DV")]
    [InlineData("Movie.DoVi P5.mkv", "DoVi P5")]
    [InlineData("Movie.DoVi P7.mkv", "DoVi P7")]
    [InlineData("Movie.SDR.mkv", null)]
    public void ParseFromFileName_DolbyVision_DetectedCorrectly(string filename, string? expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.DolbyVision.Should().Be(expected);
    }

    // ── Audio Channels Detection ─────────────────────────────────
    [Theory]
    [InlineData("Movie.7.1.Atmos.mkv", "7.1 Atmos")]
    [InlineData("Movie.7.1.mkv", "7.1")]
    [InlineData("Movie.5.1.mkv", "5.1")]
    [InlineData("Movie.Atmos.mkv", "7.1 Atmos")]
    [InlineData("Movie.mkv", "2.0 Stereo")]
    public void ParseFromFileName_AudioChannels_DetectedCorrectly(string filename, string expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.AudioChannels.Should().Be(expected);
    }

    // ── Bit Depth Detection ──────────────────────────────────────
    [Theory]
    [InlineData("Movie.10bit.mkv", "10bit")]
    [InlineData("Movie.8bit.mkv", "8bit")]
    [InlineData("Movie.mkv", "8bit")]
    public void ParseFromFileName_BitDepth_DetectedCorrectly(string filename, string expected)
    {
        var info = _extractor.ParseFromFileName(filename);
        info.BitDepth.Should().Be(expected);
    }

    // ── Combined Detection ───────────────────────────────────────
    [Fact]
    public void ParseFromFileName_ComplexFilename_DetectsAll()
    {
        var info = _extractor.ParseFromFileName("Movie.2024.2160p.DoVi P5.HDR10.DTS-HD MA.7.1.x265.10bit.mkv");
        info.Resolution.Should().Be("UHD");
        info.VideoCodec.Should().Be("HEVC");
        info.AudioCodec.Should().Be("DTS-HD MA");
        info.HdrFormat.Should().Be("HDR10");
        info.DolbyVision.Should().Be("DoVi P5");
        info.AudioChannels.Should().Be("7.1");
        info.BitDepth.Should().Be("10bit");
    }

    // ── ExtractAsync fallback ────────────────────────────────────
    [Fact]
    public async Task ExtractAsync_NonexistentFile_FallsBackToFilename()
    {
        var info = await _extractor.ExtractAsync(@"C:\nonexistent\Movie.1080p.x264.AAC.mkv");
        info.Resolution.Should().Be("1080p");
        info.VideoCodec.Should().Be("H.264");
        info.AudioCodec.Should().Be("AAC");
    }

    // ── MediaTechnicalInfo.Unknown ────────────────────────────────
    [Fact]
    public void MediaTechnicalInfo_Unknown_HasDefaults()
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
}
