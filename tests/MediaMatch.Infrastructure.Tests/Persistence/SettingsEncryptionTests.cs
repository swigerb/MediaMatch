using System.Runtime.Versioning;
using FluentAssertions;
using MediaMatch.Infrastructure.Persistence;

namespace MediaMatch.Infrastructure.Tests.Persistence;

[SupportedOSPlatform("windows")]
public sealed class SettingsEncryptionTests
{
    private readonly SettingsEncryption _sut = new();

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTrips()
    {
        const string original = "my-secret-api-key-12345";

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    public void EncryptAndDecrypt_EmptyString_ReturnsEmpty(string value)
    {
        _sut.Encrypt(value).Should().BeEmpty();
        _sut.Decrypt(value).Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_ProducesEncPrefix()
    {
        var encrypted = _sut.Encrypt("some-value");

        encrypted.Should().StartWith("ENC:");
    }

    [Fact]
    public void IsEncrypted_EncryptedValue_ReturnsTrue()
    {
        var encrypted = _sut.Encrypt("test-value");

        _sut.IsEncrypted(encrypted).Should().BeTrue();
    }

    [Theory]
    [InlineData("just-plain-text")]
    [InlineData("")]
    public void IsEncrypted_NonEncryptedValue_ReturnsFalse(string value)
    {
        _sut.IsEncrypted(value).Should().BeFalse();
    }

    [Fact]
    public void Decrypt_NonEncryptedValue_ReturnsAsIs()
    {
        const string plainValue = "not-encrypted-at-all";

        _sut.Decrypt(plainValue).Should().Be(plainValue);
    }
}
