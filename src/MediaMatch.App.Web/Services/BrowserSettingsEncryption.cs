using System.Text;
using MediaMatch.Core.Configuration;

namespace MediaMatch.App.Web.Services;

/// <summary>
/// Browser-safe <see cref="ISettingsEncryption"/> implementation.
/// </summary>
/// <remarks>
/// The browser sandbox does not expose DPAPI (Windows) or a stable per-user AES
/// key file (as the Linux/macOS heads use). To keep <c>SettingsRepository</c>
/// working without rewriting it for IndexedDB, this implementation does a
/// reversible Base64 transform — it is **obfuscation, not encryption**, and is
/// only suitable for the browser preview.
///
/// Users running the WASM head should treat any persisted API keys as
/// effectively plaintext, since anyone with access to the browser profile can
/// decode them.
/// </remarks>
public sealed class BrowserSettingsEncryption : ISettingsEncryption
{
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    public bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        try
        {
            Convert.FromBase64String(value);
            return value.Length > 0 && value.Length % 4 == 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
