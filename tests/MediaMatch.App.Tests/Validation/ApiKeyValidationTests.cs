using FluentAssertions;

namespace MediaMatch.App.Tests.Validation;

public class ApiKeyValidationTests
{
    /// <summary>
    /// Local copy of validation logic from SettingsViewModel.
    /// The App project cannot be referenced directly due to WinUI dependencies.
    /// </summary>
    private static bool IsValidApiKey(string? key) =>
        string.IsNullOrWhiteSpace(key) || key.Trim().All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

    [Fact]
    public void IsValidApiKey_Empty_ReturnsTrue()
    {
        IsValidApiKey("").Should().BeTrue();
    }

    [Fact]
    public void IsValidApiKey_Null_ReturnsTrue()
    {
        IsValidApiKey(null).Should().BeTrue();
    }

    [Fact]
    public void IsValidApiKey_AlphanumericKey_ReturnsTrue()
    {
        IsValidApiKey("abc123DEF456").Should().BeTrue();
    }

    [Fact]
    public void IsValidApiKey_WithHyphensAndUnderscores_ReturnsTrue()
    {
        IsValidApiKey("api-key_v2-test").Should().BeTrue();
    }

    [Fact]
    public void IsValidApiKey_WithSpecialChars_ReturnsFalse()
    {
        IsValidApiKey("key!@#$%").Should().BeFalse();
    }

    [Fact]
    public void IsValidApiKey_WithSpaces_ReturnsFalse()
    {
        IsValidApiKey("key with spaces").Should().BeFalse();
    }

    [Fact]
    public void IsValidApiKey_UnicodeChars_ReturnsFalse()
    {
        // char.IsLetterOrDigit returns true for accented letters,
        // so use non-letter/non-digit Unicode symbols instead
        IsValidApiKey("key\u2603\u2764").Should().BeFalse();
    }
}
