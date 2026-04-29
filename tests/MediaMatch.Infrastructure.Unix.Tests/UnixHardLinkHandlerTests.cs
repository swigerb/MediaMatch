using FluentAssertions;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Unix.FileSystem;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class UnixHardLinkHandlerTests
{
    private readonly UnixHardLinkHandler _handler = new();

    [Fact]
    public void TryCreateHardLink_NonExistentTarget_ReturnsFalse()
    {
        var result = _handler.TryCreateHardLink(
            Path.Combine(Path.GetTempPath(), "nonexistent-link"),
            Path.Combine(Path.GetTempPath(), "nonexistent-target-" + Guid.NewGuid()));
        result.Should().BeFalse();
    }

    [Fact]
    public void ImplementsIHardLinkHandler()
    {
        _handler.Should().BeAssignableTo<IHardLinkHandler>();
    }
}
