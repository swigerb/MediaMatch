using FluentAssertions;
using MediaMatch.Core.Configuration;
using Moq;

namespace MediaMatch.Infrastructure.Tests.Persistence;

/// <summary>
/// Passthrough encryption for testing — returns values unchanged.
/// </summary>
internal sealed class PassthroughEncryption : ISettingsEncryption
{
    public int EncryptCallCount { get; private set; }
    public int DecryptCallCount { get; private set; }

    public string Encrypt(string plainText)
    {
        EncryptCallCount++;
        return plainText;
    }

    public string Decrypt(string cipherText)
    {
        DecryptCallCount++;
        return cipherText;
    }

    public bool IsEncrypted(string value) => false;
}

public sealed class SettingsRepositoryTests
{
    [Fact]
    public void PassthroughEncryption_Encrypt_ReturnsOriginal()
    {
        var enc = new PassthroughEncryption();

        enc.Encrypt("api-key-123").Should().Be("api-key-123");
        enc.EncryptCallCount.Should().Be(1);
    }

    [Fact]
    public void PassthroughEncryption_Decrypt_ReturnsOriginal()
    {
        var enc = new PassthroughEncryption();

        enc.Decrypt("api-key-123").Should().Be("api-key-123");
        enc.DecryptCallCount.Should().Be(1);
    }

    [Fact]
    public void PassthroughEncryption_IsEncrypted_ReturnsFalse()
    {
        var enc = new PassthroughEncryption();

        enc.IsEncrypted("anything").Should().BeFalse();
    }

    [Fact]
    public void MockEncryption_EncryptDecrypt_CalledCorrectly()
    {
        var mock = new Mock<ISettingsEncryption>();
        mock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");
        mock.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s.Replace("ENC:", ""));
        mock.Setup(e => e.IsEncrypted(It.IsAny<string>())).Returns<string>(s => s.StartsWith("ENC:"));

        var encrypted = mock.Object.Encrypt("secret");
        var decrypted = mock.Object.Decrypt(encrypted);

        encrypted.Should().Be("ENC:secret");
        decrypted.Should().Be("secret");
        mock.Verify(e => e.Encrypt("secret"), Times.Once);
        mock.Verify(e => e.Decrypt("ENC:secret"), Times.Once);
    }

    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        settings.CacheDurationMinutes.Should().Be(60);
        settings.ApiKeys.Should().NotBeNull();
        settings.ApiKeys.TmdbApiKey.Should().BeEmpty();
        settings.ApiKeys.TvdbApiKey.Should().BeEmpty();
        settings.ApiKeys.OpenSubtitlesApiKey.Should().BeEmpty();
        settings.RenamePatterns.Should().NotBeNull();
        settings.OutputFolders.Should().NotBeNull();
    }
}
