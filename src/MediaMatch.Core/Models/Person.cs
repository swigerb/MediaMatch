namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a person involved in a media production (cast or crew).
/// </summary>
/// <param name="Name">The person's full name.</param>
/// <param name="Character">The character name for cast members.</param>
/// <param name="Department">The department for crew members (e.g., "Directing").</param>
/// <param name="Job">The specific job title for crew members (e.g., "Director").</param>
/// <param name="TmdbId">The TMDb person identifier.</param>
/// <param name="ProfileUrl">The URL of the person's profile image.</param>
/// <param name="Order">The display order for cast billing.</param>
public sealed record Person(
    string Name,
    string? Character = null,
    string? Department = null,
    string? Job = null,
    int? TmdbId = null,
    string? ProfileUrl = null,
    int? Order = null);
