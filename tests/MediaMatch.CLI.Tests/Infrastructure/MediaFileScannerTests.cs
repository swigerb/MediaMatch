using FluentAssertions;
using MediaMatch.CLI.Infrastructure;

namespace MediaMatch.CLI.Tests.Infrastructure;

public sealed class MediaFileScannerTests
{
    [Fact]
    public void Scan_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            var result = MediaFileScanner.Scan(tempDir.FullName, recursive: false);

            result.Should().BeEmpty();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Scan_DirectoryWithMediaFiles_ReturnsMediaPaths()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "movie.mkv"), []);
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "clip.mp4"), []);

            var result = MediaFileScanner.Scan(tempDir.FullName, recursive: false);

            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("movie.mkv"));
            result.Should().Contain(f => f.EndsWith("clip.mp4"));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Scan_NonRecursive_IgnoresSubdirectories()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "root.mkv"), []);
            var sub = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllBytes(Path.Combine(sub.FullName, "nested.mkv"), []);

            var result = MediaFileScanner.Scan(tempDir.FullName, recursive: false);

            result.Should().ContainSingle()
                .Which.Should().EndWith("root.mkv");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Scan_Recursive_IncludesSubdirectories()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "root.mkv"), []);
            var sub = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllBytes(Path.Combine(sub.FullName, "nested.mp4"), []);

            var result = MediaFileScanner.Scan(tempDir.FullName, recursive: true);

            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("root.mkv"));
            result.Should().Contain(f => f.EndsWith("nested.mp4"));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Scan_SingleFile_ReturnsThatFile()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            var filePath = Path.Combine(tempDir.FullName, "single.avi");
            File.WriteAllBytes(filePath, []);

            var result = MediaFileScanner.Scan(filePath, recursive: false);

            result.Should().ContainSingle()
                .Which.Should().Be(filePath);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Scan_NonMediaFiles_AreExcluded()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "readme.txt"), []);
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "data.csv"), []);
            File.WriteAllBytes(Path.Combine(tempDir.FullName, "image.png"), []);

            var result = MediaFileScanner.Scan(tempDir.FullName, recursive: false);

            result.Should().BeEmpty();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
