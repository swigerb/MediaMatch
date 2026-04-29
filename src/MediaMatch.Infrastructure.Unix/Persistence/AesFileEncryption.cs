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

    private static readonly UnixFileMode KeyFileMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private readonly string _keyFilePath;
    private readonly ILogger<AesFileEncryption> _logger;
    private readonly object _keyLock = new();
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
        // Fast path: cached key already loaded.
        var cached = Volatile.Read(ref _cachedKey);
        if (cached is not null)
            return cached;

        // Serialize key initialization across threads — without this, two threads
        // racing in this method could each generate (and persist) a fresh key,
        // causing one to overwrite the other and breaking decryption of values
        // encrypted with the discarded key.
        lock (_keyLock)
        {
            if (_cachedKey is not null)
                return _cachedKey;

            if (File.Exists(_keyFilePath))
            {
                EnsureRestrictivePermissions(_keyFilePath);
                var existing = File.ReadAllBytes(_keyFilePath);
                if (existing.Length == KeySizeBytes)
                {
                    _cachedKey = existing;
                    return _cachedKey;
                }

                _logger.LogWarning(
                    "Key file has unexpected size ({Size} bytes), regenerating",
                    existing.Length);
                File.Delete(_keyFilePath);
            }

            var key = new byte[KeySizeBytes];
            RandomNumberGenerator.Fill(key);

            var keyDir = Path.GetDirectoryName(_keyFilePath)!;
            Directory.CreateDirectory(keyDir);
            WriteKeyFile(_keyFilePath, key);

            _logger.LogInformation("Generated new encryption key at {Path}", _keyFilePath);
            _cachedKey = key;
            return key;
        }
    }

    private static void WriteKeyFile(string path, byte[] key)
    {
        // Use FileStreamOptions so the file is created atomically with 0600
        // permissions, closing the umask race window where File.WriteAllBytes
        // followed by File.SetUnixFileMode briefly leaves the key world-readable
        // (and leaves it permanently exposed if the process crashes between
        // the write and the chmod).
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                UnixCreateMode = KeyFileMode,
            };
            using var fs = new FileStream(path, options);
            fs.Write(key);
        }
        else
        {
            // Non-Unix path (e.g. running tests on Windows) — UnixCreateMode is
            // not supported. Permissions are not a concern on Windows here.
            File.WriteAllBytes(path, key);
        }
    }

    private void EnsureRestrictivePermissions(string path)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        try
        {
            var current = File.GetUnixFileMode(path);
            if (current != KeyFileMode)
            {
                _logger.LogWarning(
                    "Key file {Path} had permissions {Current}; restricting to owner-only",
                    path, current);
                File.SetUnixFileMode(path, KeyFileMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to verify permissions on {Path}", path);
        }
    }

    private static string GetDefaultKeyFilePath()
    {
        // Store key alongside settings: ~/.local/share/MediaMatch/ on Linux,
        // ~/Library/Application Support/MediaMatch/ on macOS
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "MediaMatch", ".encryption-key");
    }
}
