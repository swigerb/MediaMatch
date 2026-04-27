namespace MediaMatch.Core.Models;

public sealed record Movie(
    string Name,
    int Year,
    int? TmdbId = null,
    int? ImdbId = null,
    string? Language = null);
