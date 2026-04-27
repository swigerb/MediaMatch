namespace MediaMatch.Core.Configuration;

/// <summary>
/// Encrypts and decrypts sensitive string values (API keys) at rest.
/// </summary>
public interface ISettingsEncryption
{
    /// <summary>Encrypts a plaintext value. Returns a Base64-encoded ciphertext.</summary>
    string Encrypt(string plainText);

    /// <summary>Decrypts a Base64-encoded ciphertext back to plaintext.</summary>
    string Decrypt(string cipherText);

    /// <summary>Returns true if the value looks like an encrypted token (Base64 format).</summary>
    bool IsEncrypted(string value);
}
