using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Services;

namespace MediaMatch.Application.Tests.Services;

public sealed class ChecksumServiceTests
{
    private readonly IChecksumService _service = new ChecksumService();

    [Fact]
    public async Task ComputeAsync_Crc32_ReturnsExpectedHash()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Hello, World!");

            // Act
            var hash = await _service.ComputeAsync(tempFile, ChecksumAlgorithm.Crc32);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().HaveLength(8, "CRC32 is 4 bytes = 8 hex chars");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeAsync_Md5_ReturnsExpectedHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Hello, World!");

            var hash = await _service.ComputeAsync(tempFile, ChecksumAlgorithm.Md5);

            hash.Should().NotBeNullOrEmpty();
            hash.Should().HaveLength(32, "MD5 is 16 bytes = 32 hex chars");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeAsync_Sha256_ReturnsExpectedHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Hello, World!");

            var hash = await _service.ComputeAsync(tempFile, ChecksumAlgorithm.Sha256);

            hash.Should().NotBeNullOrEmpty();
            hash.Should().HaveLength(64, "SHA256 is 32 bytes = 64 hex chars");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeAsync_ReportsProgress()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Write enough data that progress gets reported multiple times
            await File.WriteAllBytesAsync(tempFile, new byte[500_000]);

            var progressValues = new List<double>();
            var progress = new SynchronousProgress<double>(p => progressValues.Add(p));

            await _service.ComputeAsync(tempFile, ChecksumAlgorithm.Sha256, progress);

            // Progress should have been reported at least once
            progressValues.Should().NotBeEmpty();
            progressValues.Last().Should().BeApproximately(1.0, 0.01);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// IProgress implementation that invokes callback synchronously (no SynchronizationContext).
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    [Fact]
    public async Task VerifyAsync_CorrectHash_ReturnsTrue()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test data");

            var hash = await _service.ComputeAsync(tempFile, ChecksumAlgorithm.Md5);
            var result = await _service.VerifyAsync(tempFile, hash, ChecksumAlgorithm.Md5);

            result.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyAsync_WrongHash_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test data");

            var result = await _service.VerifyAsync(tempFile, "00000000000000000000000000000000", ChecksumAlgorithm.Md5);

            result.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => _service.ComputeAsync(@"C:\nonexistent\file.bin", ChecksumAlgorithm.Crc32);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Theory]
    [InlineData(ChecksumAlgorithm.Crc32)]
    [InlineData(ChecksumAlgorithm.Md5)]
    [InlineData(ChecksumAlgorithm.Sha1)]
    [InlineData(ChecksumAlgorithm.Sha256)]
    [InlineData(ChecksumAlgorithm.Sha512)]
    public async Task ComputeAsync_AllAlgorithms_ReturnDeterministicHash(ChecksumAlgorithm algorithm)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "deterministic content");

            var hash1 = await _service.ComputeAsync(tempFile, algorithm);
            var hash2 = await _service.ComputeAsync(tempFile, algorithm);

            hash1.Should().Be(hash2, "same file + same algorithm should produce identical hash");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
