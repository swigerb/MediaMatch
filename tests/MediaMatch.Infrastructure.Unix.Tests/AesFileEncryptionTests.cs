using FluentAssertions;
using MediaMatch.Infrastructure.Unix.Persistence;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class AesFileEncryptionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AesFileEncryption _encryption;

    public AesFileEncryptionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MediaMatch_AesTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        var keyPath = Path.Combine(_tempDir, ".encryption-key");
        _encryption = new AesFileEncryption(keyPath);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        _encryption.Encrypt("").Should().Be("");
    }

    [Fact]
    public void Encrypt_Null_ReturnsNull()
    {
        _encryption.Encrypt(null!).Should().BeNull();
    }

    [Fact]
    public void Encrypt_ProducesEncryptedPrefix()
    {
        var result = _encryption.Encrypt("my-api-key");
        result.Should().StartWith("ENC:");
    }

    [Fact]
    public void Decrypt_RoundTrips()
    {
        var original = "super-secret-api-key-12345";
        var encrypted = _encryption.Encrypt(original);
        var decrypted = _encryption.Decrypt(encrypted);
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmpty()
    {
        _encryption.Decrypt("").Should().Be("");
    }

    [Fact]
    public void Decrypt_UnencryptedString_ReturnsAsIs()
    {
        _encryption.Decrypt("plain-text").Should().Be("plain-text");
    }

    [Fact]
    public void IsEncrypted_EncryptedValue_ReturnsTrue()
    {
        var encrypted = _encryption.Encrypt("test");
        _encryption.IsEncrypted(encrypted).Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_PlainValue_ReturnsFalse()
    {
        _encryption.IsEncrypted("plain-text").Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_EmptyString_ReturnsFalse()
    {
        _encryption.IsEncrypted("").Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_Null_ReturnsFalse()
    {
        _encryption.IsEncrypted(null!).Should().BeFalse();
    }

    [Fact]
    public void Encrypt_CreatesKeyFile()
    {
        var keyPath = Path.Combine(_tempDir, ".key-creation-test");
        var enc = new AesFileEncryption(keyPath);
        enc.Encrypt("trigger-key-creation");
        File.Exists(keyPath).Should().BeTrue();
        File.ReadAllBytes(keyPath).Length.Should().Be(32); // AES-256
    }

    [Fact]
    public void Encrypt_SameValueProducesDifferentCiphertexts()
    {
        var value = "same-value";
        var enc1 = _encryption.Encrypt(value);
        var enc2 = _encryption.Encrypt(value);
        enc1.Should().NotBe(enc2, "each encryption uses a random nonce");
    }

    [Fact]
    public void Decrypt_BothCiphertextsDecryptCorrectly()
    {
        var value = "test-value";
        var enc1 = _encryption.Encrypt(value);
        var enc2 = _encryption.Encrypt(value);
        _encryption.Decrypt(enc1).Should().Be(value);
        _encryption.Decrypt(enc2).Should().Be(value);
    }

    [Fact]
    public void Decrypt_WithExistingKeyFile_RoundTrips()
    {
        var keyPath = Path.Combine(_tempDir, ".persist-key-test");
        var enc1 = new AesFileEncryption(keyPath);
        var encrypted = enc1.Encrypt("persistent-secret");

        // Create a new instance that reads the same key file
        var enc2 = new AesFileEncryption(keyPath);
        enc2.Decrypt(encrypted).Should().Be("persistent-secret");
    }

    [Fact]
    public void Decrypt_CorruptedCiphertext_ReturnsCiphertext()
    {
        // Corrupted but starts with ENC: prefix
        var corrupted = "ENC:" + Convert.ToBase64String(new byte[50]);
        var result = _encryption.Decrypt(corrupted);
        result.Should().Be(corrupted, "corrupted data should be returned as-is");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}
