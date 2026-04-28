using FluentAssertions;
using MediaMatch.Infrastructure.Platform;

namespace MediaMatch.Infrastructure.Tests.Platform;

public sealed class PlatformServiceTests
{
    private readonly PlatformService _sut = new();

    [Fact]
    public void IsWindows_ReturnsTrue_OnWindows()
    {
        // This test suite runs on Windows
        _sut.IsWindows.Should().BeTrue();
    }

    [Fact]
    public void IsMacOS_ReturnsFalse_OnWindows()
    {
        _sut.IsMacOS.Should().BeFalse();
    }

    [Fact]
    public void IsLinux_ReturnsFalse_OnWindows()
    {
        _sut.IsLinux.Should().BeFalse();
    }

    [Fact]
    public void PlatformName_IsNotEmpty()
    {
        _sut.PlatformName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PlatformName_ReturnsWindows_OnWindows()
    {
        _sut.PlatformName.Should().Be("Windows");
    }

    [Fact]
    public void GetSettingsDirectory_ReturnsValidPath()
    {
        var path = _sut.GetSettingsDirectory();

        path.Should().NotBeNullOrWhiteSpace();
        path.Should().Contain("MediaMatch");
    }

    [Fact]
    public void GetSettingsDirectory_UsesLocalAppData_OnWindows()
    {
        var path = _sut.GetSettingsDirectory();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        path.Should().StartWith(localAppData);
    }

    [Fact]
    public void SupportsHardLinks_ReturnsTrue_OnWindows()
    {
        _sut.SupportsHardLinks.Should().BeTrue();
    }

    [Fact]
    public void SupportsReFsClone_ReturnsTrue_OnWindows()
    {
        _sut.SupportsReFsClone.Should().BeTrue();
    }
}
