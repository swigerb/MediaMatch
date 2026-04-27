using FluentAssertions;
using MediaMatch.CLI.Commands;

namespace MediaMatch.CLI.Tests.Commands;

public class MatchCommandTests
{
    [Fact]
    public void Validate_EmptyPath_ReturnsError()
    {
        var settings = new MatchSettings { Path = "   " };

        var result = settings.Validate();

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("--path is required");
    }

    [Fact]
    public void Validate_NonexistentPath_ReturnsError()
    {
        var settings = new MatchSettings { Path = @"C:\__nonexistent_path_12345__" };

        var result = settings.Validate();

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Path not found");
    }

    [Fact]
    public void Validate_ExistingDirectory_ReturnsSuccess()
    {
        var tempDir = Directory.CreateTempSubdirectory("MediaMatch_test_");
        try
        {
            var settings = new MatchSettings { Path = tempDir.FullName };

            var result = settings.Validate();

            result.Successful.Should().BeTrue();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void DefaultFormat_IsTable()
    {
        var settings = new MatchSettings { Path = "dummy" };

        settings.Format.Should().Be("table");
    }

    [Fact]
    public void DefaultRecursive_IsFalse()
    {
        var settings = new MatchSettings { Path = "dummy" };

        settings.Recursive.Should().BeFalse();
    }
}
