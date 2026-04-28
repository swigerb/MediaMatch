using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using MediaMatch.Core.Configuration;

#pragma warning disable CA1416 // Platform compatibility — class is [SupportedOSPlatform("windows")]

namespace MediaMatch.Infrastructure.Persistence;

/// <summary>
/// Encrypts and decrypts API key values using Windows DPAPI (current-user scope).
/// Only API key fields are encrypted — the rest of settings.json stays readable.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SettingsEncryption : ISettingsEncryption
{
    private const string EncryptedPrefix = "ENC:";

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return EncryptedPrefix + Convert.ToBase64String(cipherBytes);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || !IsEncrypted(cipherText))
            return cipherText;

        var base64 = cipherText[EncryptedPrefix.Length..];
        var cipherBytes = Convert.FromBase64String(base64);
        var plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <inheritdoc />
    public bool IsEncrypted(string value)
        => !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
}
