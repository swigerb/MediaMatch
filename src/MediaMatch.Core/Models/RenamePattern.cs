namespace MediaMatch.Core.Models;

/// <summary>
/// A rename pattern template with description and example output for UI display.
/// </summary>
public sealed record RenamePattern(
    string Template,
    string Description,
    string ExampleOutput);
