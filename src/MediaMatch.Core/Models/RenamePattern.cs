namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a rename pattern template with description and example output for UI display.
/// </summary>
/// <param name="Template">The rename pattern template string with token placeholders.</param>
/// <param name="Description">The human-readable description of the pattern.</param>
/// <param name="ExampleOutput">An example filename produced by this pattern.</param>
public sealed record RenamePattern(
    string Template,
    string Description,
    string ExampleOutput);
