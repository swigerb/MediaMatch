using FluentAssertions;

namespace MediaMatch.App.Tests.Validation;

public sealed class ApiKeyValidationTests
{
    /// <summary>
    /// Local copy of validation logic from SettingsViewModel.
    /// The App project cannot be referenced directly due to WinUI dependencies.
    /// </summary>
    private static bool IsValidApiKey(string? key) =>
        string.IsNullOrWhiteSpace(key) || key.Trim().All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc123DEF456")]
    [InlineData("api-key_v2-test")]
    public void IsValidApiKey_ValidInput_ReturnsTrue(string? key)
    {
        IsValidApiKey(key).Should().BeTrue();
    }

    [Theory]
    [InlineData("key!@#$%")]
    [InlineData("key with spaces")]
    [InlineData("key\u2603\u2764")]
    public void IsValidApiKey_InvalidInput_ReturnsFalse(string key)
    {
        IsValidApiKey(key).Should().BeFalse();
    }
}
