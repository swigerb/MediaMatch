using System.Security.Cryptography;
using System.Text;
using MediaMatch.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Unix.Persistence;

/// <summary>
/// Cross-platform encryption for settings using AES-256-GCM with a machine-scoped key file.
/// Used on macOS and Linux where DPAPI is not available.
/// The key file is stored in a platform-appropriate location with restricted permissions.
/// </summary>
public sealed class AesFileEncryption : ISettingsEncryption
{
    private const string EncryptedPrefix = "ENC:";
    private const int KeySizeBytes = 32; // AES-256
    private const int NonceSizeBytes = 12; // GCM standard
    private const int TagSizeBytes = 16; // GCM standard

    private readonly string _keyFilePath;
    private readonly ILogger<AesFileEncryption> _logger;
    private byte[]? _cachedKey;

    public AesFileEncryption(ILogger<AesFileEncryption>? logger = null)
        : this(GetDefaultKeyFilePath(), logger)
    {
    }

    public AesFileEncryption(string keyFilePath, ILogger<AesFileEncryption>? logger = null)
    {
        _keyFilePath = keyFilePath;
        _logger = logger ?? NullLogger<AesFileEncryption>.Instance;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var key = GetOrCreateKey();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: nonce + tag + ciphertext
        var combined = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, NonceSizeBytes);
        cipherBytes.CopyTo(combined, NonceSizeBytes + TagSizeBytes);

        return EncryptedPrefix + Convert.ToBase64String(combined);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || !IsEncrypted(cipherText))
            return cipherText;

        try
        {
            var key = GetOrCreateKey();
            var combined = Convert.FromBase64String(cipherText[EncryptedPrefix.Length..]);

            if (combined.Length < NonceSizeBytes + TagSizeBytes)
                throw new CryptographicException("Ciphertext is too short.");

            var nonce = combined[..NonceSizeBytes];
            var tag = combined[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
            var cipherBytes = combined[(NonceSizeBytes + TagSizeBytes)..];

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt value — returning as-is");
            return cipherText;
        }
    }

    /// <inheritdoc />
    public bool IsEncrypted(string value)
        => !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

    private byte[] GetOrCreateKey()
    {
        if (_cachedKey is not null)
            return _cachedKey;

        if (File.Exists(_keyFilePath))
        {
            _cachedKey = File.ReadAllBytes(_keyFilePath);
            if (_cachedKey.Length == KeySizeBytes)
                return _cachedKey;

            _logger.LogWarning("Key file has unexpected size ({Size} bytes), regenerating", _cachedKey.Length);
        }

        // Generate a new key
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);

        // Save with restricted permissions
        var keyDir = Path.GetDirectoryName(_keyFilePath)!;
        Directory.CreateDirectory(keyDir);
        File.WriteAllBytes(_keyFilePath, key);

        // On Unix, restrict permissions to owner only (0600)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(_keyFilePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        _logger.LogInformation("Generated new encryption key at {Path}", _keyFilePath);
        _cachedKey = key;
        return key;
    }

    private static string GetDefaultKeyFilePath()
    {
        // Store key alongside settings: ~/.local/share/MediaMatch/ on Linux,
        // ~/Library/Application Support/MediaMatch/ on macOS
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "MediaMatch", ".encryption-key");
    }
}
