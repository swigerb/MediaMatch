namespace MediaMatch.Core.Configuration;

/// <summary>
/// Encrypts and decrypts sensitive string values (API keys) at rest.
/// </summary>
public interface ISettingsEncryption
{
    /// <summary>Encrypts a plaintext value. Returns a Base64-encoded ciphertext.</summary>
    /// <param name="plainText">The plaintext value to encrypt.</param>
    /// <returns>The Base64-encoded ciphertext.</returns>
    string Encrypt(string plainText);

    /// <summary>Decrypts a Base64-encoded ciphertext back to plaintext.</summary>
    /// <param name="cipherText">The Base64-encoded ciphertext to decrypt.</param>
    /// <returns>The decrypted plaintext value.</returns>
    string Decrypt(string cipherText);

    /// <summary>Returns true if the value looks like an encrypted token (Base64 format).</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A value indicating whether the value appears to be encrypted.</returns>
    bool IsEncrypted(string value);
}
