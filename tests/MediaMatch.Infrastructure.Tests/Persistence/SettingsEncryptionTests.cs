using System.Runtime.Versioning;
using FluentAssertions;
using MediaMatch.Infrastructure.Persistence;

namespace MediaMatch.Infrastructure.Tests.Persistence;

[SupportedOSPlatform("windows")]
public class SettingsEncryptionTests
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

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        _sut.Encrypt(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmpty()
    {
        _sut.Decrypt(string.Empty).Should().BeEmpty();
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

    [Fact]
    public void IsEncrypted_PlainValue_ReturnsFalse()
    {
        _sut.IsEncrypted("just-plain-text").Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_EmptyString_ReturnsFalse()
    {
        _sut.IsEncrypted(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Decrypt_NonEncryptedValue_ReturnsAsIs()
    {
        const string plainValue = "not-encrypted-at-all";

        _sut.Decrypt(plainValue).Should().Be(plainValue);
    }
}
