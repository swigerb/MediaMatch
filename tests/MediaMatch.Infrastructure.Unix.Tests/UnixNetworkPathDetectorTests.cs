using FluentAssertions;
using MediaMatch.Infrastructure.Unix.FileSystem;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class UnixNetworkPathDetectorTests
{
    private readonly UnixNetworkPathDetector _detector = new();

    [Fact]
    public void IsNetworkPath_ThrowsOnNullOrWhitespace()
    {
        var act = () => _detector.IsNetworkPath(null!);
        act.Should().Throw<ArgumentException>();

        var act2 = () => _detector.IsNetworkPath("  ");
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsNetworkPath_LocalPath_ReturnsFalse()
    {
        // On Windows (where tests run), /proc/mounts doesn't exist, so should return false
        var result = _detector.IsNetworkPath(@"C:\Users\test\file.txt");
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNetworkPath_RelativePath_ReturnsFalse()
    {
        var result = _detector.IsNetworkPath("./relative/path");
        result.Should().BeFalse();
    }
}
