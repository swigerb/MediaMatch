namespace MediaMatch.App.macOS.ViewModels;

/// <summary>
/// Describes a single expression token available for insertion.
/// </summary>
/// <param name="Name">The token syntax (e.g., "{n}").</param>
/// <param name="Description">A human-readable description of the token.</param>
/// <param name="Category">The token category (e.g., Common, Series, Movie).</param>
public sealed record TokenInfo(string Name, string Description, string Category);
